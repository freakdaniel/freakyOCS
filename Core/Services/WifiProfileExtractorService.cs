using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.Services;

/// <summary>
/// Extracts saved WiFi profiles and passwords from the operating system.
/// Ported from wifi_profile_extractor.py.
/// </summary>
public sealed class WifiProfileExtractorService
{
    private readonly ProcessRunner _runner;
    private readonly ILogger<WifiProfileExtractorService>? _logger;

    public WifiProfileExtractorService(ProcessRunner runner, ILogger<WifiProfileExtractorService>? logger = null)
    {
        _runner = runner;
        _logger = logger;
    }

    /// <summary>
    /// Extracts all saved WiFi profiles with passwords from the current OS.
    /// </summary>
    public async Task<List<WifiProfile>> GetProfilesAsync(CancellationToken ct = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return await GetWindowsProfilesAsync(ct);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return await GetLinuxProfilesAsync(ct);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return await GetMacOsProfilesAsync(ct);

        _logger?.LogWarning("Unsupported OS for WiFi profile extraction");
        return [];
    }

    private async Task<List<WifiProfile>> GetWindowsProfilesAsync(CancellationToken ct)
    {
        var profiles = new List<WifiProfile>();

        var result = await _runner.RunAsync("netsh", ["wlan", "show", "profiles"], cancellationToken: ct);
        if (!result.Success) return profiles;

        var ssidList = new List<string>();
        foreach (var line in result.Output.Split('\n'))
        {
            if (line.Contains("All User Profile"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    var ssid = parts[1].Trim();
                    if (!string.IsNullOrEmpty(ssid))
                        ssidList.Add(ssid);
                }
            }
        }

        foreach (var ssid in ssidList)
        {
            if (ct.IsCancellationRequested) break;
            var password = await GetWindowsPasswordAsync(ssid, ct);
            if (password is not null)
                profiles.Add(new WifiProfile(ssid, password));
        }

        return profiles;
    }

    private async Task<string?> GetWindowsPasswordAsync(string ssid, CancellationToken ct)
    {
        var result = await _runner.RunAsync("netsh",
            ["wlan", "show", "profile", ssid, "key=clear"], cancellationToken: ct);
        if (!result.Success) return null;

        string? authType = null;
        string? password = null;

        foreach (var line in result.Output.Split('\n'))
        {
            if (authType is null && line.Contains("Authentication"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                    authType = GetAuthenticationType(parts[1].Trim());
            }
            else if (line.Contains("Key Content"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                    password = parts[1].Trim();
            }
        }

        return ValidatePassword(authType, password);
    }

    private async Task<List<WifiProfile>> GetLinuxProfilesAsync(CancellationToken ct)
    {
        var profiles = new List<WifiProfile>();

        var result = await _runner.RunAsync("nmcli",
            ["-t", "-f", "NAME", "connection", "show"], cancellationToken: ct);
        if (!result.Success) return profiles;

        var ssidList = result.Output.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        foreach (var ssid in ssidList)
        {
            if (ct.IsCancellationRequested) break;
            var password = await GetLinuxPasswordAsync(ssid, ct);
            if (password is not null)
                profiles.Add(new WifiProfile(ssid, password));
        }

        return profiles;
    }

    private async Task<string?> GetLinuxPasswordAsync(string ssid, CancellationToken ct)
    {
        var result = await _runner.RunAsync("nmcli",
            ["--show-secrets", "connection", "show", ssid], cancellationToken: ct);
        if (!result.Success) return null;

        string? authType = null;
        string? password = null;

        foreach (var line in result.Output.Split('\n'))
        {
            if (line.Contains("802-11-wireless-security.key-mgmt:"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                    authType = GetAuthenticationType(parts[1].Trim());
            }
            else if (line.Contains("802-11-wireless-security.psk:"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                    password = parts[1].Trim();
            }
        }

        return ValidatePassword(authType, password);
    }

    private async Task<List<WifiProfile>> GetMacOsProfilesAsync(CancellationToken ct)
    {
        var profiles = new List<WifiProfile>();

        // Find WiFi interfaces
        var hwResult = await _runner.RunAsync("networksetup",
            ["listallhardwareports"], cancellationToken: ct);
        if (!hwResult.Success) return profiles;

        var interfaces = new List<string>();
        foreach (var block in hwResult.Output.Split("\n\n"))
        {
            if (!block.Contains("Device: en")) continue;
            var match = System.Text.RegularExpressions.Regex.Match(block, @"Device: (en\d+)");
            if (!match.Success) continue;

            var iface = match.Groups[1].Value;
            var testResult = await _runner.RunAsync("networksetup",
                ["listpreferredwirelessnetworks", iface], cancellationToken: ct);
            if (testResult.Success && testResult.Output.Contains("Preferred networks on"))
                interfaces.Add(iface);
        }

        foreach (var iface in interfaces)
        {
            var listResult = await _runner.RunAsync("networksetup",
                ["listpreferredwirelessnetworks", iface], cancellationToken: ct);
            if (!listResult.Success) continue;

            var ssidList = listResult.Output.Split('\n')
                .Skip(1)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            foreach (var ssid in ssidList)
            {
                if (ct.IsCancellationRequested) break;
                var password = await GetMacOsPasswordAsync(ssid, ct);
                if (password is not null)
                {
                    profiles.Add(new WifiProfile(ssid, password));
                }
            }

            if (profiles.Count > 0) break; // Found profiles on this interface
        }

        return profiles;
    }

    private async Task<string?> GetMacOsPasswordAsync(string ssid, CancellationToken ct)
    {
        var result = await _runner.RunAsync("security",
            ["find-generic-password", "-wa", ssid], cancellationToken: ct);
        if (!result.Success) return null;

        var password = result.Output.Trim();
        return string.IsNullOrEmpty(password) ? null : ValidatePassword("wpa", password);
    }

    private static string? GetAuthenticationType(string authType)
    {
        var lower = authType.ToLowerInvariant();

        if (lower.Contains("none") || lower.Contains("owe") || lower.Contains("open"))
            return "open";
        if (lower.Contains("wep") || lower.Contains("shared"))
            return "wep";
        if (lower.Contains("wpa") || lower.Contains("sae"))
            return "wpa";

        return null;
    }

    private static string? ValidatePassword(string? authType, string? password)
    {
        if (password is null) return null;
        if (authType is null) return password;
        if (authType == "open") return "";

        // Check ASCII only
        if (!password.All(c => c >= 32 && c <= 126))
            return null;

        if (password.Length >= 8 && password.Length <= 63)
            return password;

        return null;
    }
}

public record WifiProfile(string Ssid, string Password);
