using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Datasets;

namespace OcsNet.Core.Services;

/// <summary>
/// Service for downloading and gathering OpenCore resources:
/// kexts, OpenCore binaries, ACPI patches, drivers, tools, etc.
/// </summary>
public sealed class FileGatheringService
{
    private readonly DownloadService _download;
    private readonly GitHubService _github;
    private readonly HashService _hash;
    private readonly AppUtils _utils;
    private readonly ILogger<FileGatheringService>? _logger;

    private const string DortaniaBuildsUrl = "https://dortania.github.io/build-repo/";
    private const string AcidicPluginsUrl = "https://raw.githubusercontent.com/acidanthera/";

    public FileGatheringService(
        DownloadService download,
        GitHubService github,
        HashService hash,
        AppUtils utils,
        ILogger<FileGatheringService>? logger = null)
    {
        _download = download;
        _github = github;
        _hash = hash;
        _utils = utils;
        _logger = logger;
    }

    public async Task<string?> DownloadOpenCoreAsync(
        string version,
        string variant, // "DEBUG" or "RELEASE"
        string outputDir,
        CancellationToken ct = default)
    {
        var fileName = $"OpenCore-{version}-{variant}.zip";
        var manifestUrl = $"{DortaniaBuildsUrl}manifest.json";

        try
        {
            var manifestText = await _download.FetchTextAsync(manifestUrl, ct);
            if (manifestText is null)
                return null;

            // Parse manifest to find OpenCore
            var manifest = JsonDocument.Parse(manifestText);
            if (!manifest.RootElement.TryGetProperty("OpenCorePkg", out var ocPkg))
                return null;

            // Get versions array
            if (!ocPkg.TryGetProperty("versions", out var versions))
                return null;

            // Find matching version
            JsonElement? targetVersion = null;
            foreach (var ver in versions.EnumerateArray())
            {
                if (ver.TryGetProperty("version", out var verStr) &&
                    verStr.GetString() == version)
                {
                    targetVersion = ver;
                    break;
                }
            }

            if (targetVersion is null)
            {
                // Use latest if version not found
                if (versions.GetArrayLength() > 0)
                    targetVersion = versions[0];
                else
                    return null;
            }

            // Get download URL
            var links = targetVersion.Value.GetProperty("links");
            var variantKey = variant.ToUpperInvariant() == "DEBUG" ? "debug" : "release";
            if (!links.TryGetProperty(variantKey, out var downloadUrl))
                return null;

            var url = downloadUrl.GetString();
            if (string.IsNullOrEmpty(url))
                return null;

            var outputPath = Path.Combine(outputDir, fileName);
            var success = await _download.DownloadFileAsync(url, outputPath, ct: ct);

            return success ? outputPath : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download OpenCore {Version}", version);
            return null;
        }
    }

    public async Task<string?> DownloadKextAsync(
        KextInfo kext,
        string outputDir,
        string? specificVersion = null,
        CancellationToken ct = default)
    {
        try
        {
            string? downloadUrl = null;
            string fileName;

            if (kext.GithubRepo is not null)
            {
                // Fetch from GitHub releases
                var releases = await _github.GetReleasesAsync(kext.GithubRepo.Owner, kext.GithubRepo.Repo, ct);
                if (releases.Count == 0)
                    return null;

                var targetRelease = specificVersion is not null
                    ? releases.FirstOrDefault(r => r.TagName.Contains(specificVersion))
                    : releases.FirstOrDefault();

                if (targetRelease is null)
                    return null;

                // Find the kext asset
                var kextAsset = targetRelease.Assets
                    .FirstOrDefault(a => a.Name.EndsWith(".zip") &&
                                         (a.Name.Contains(kext.Name) ||
                                          a.Name.Contains("RELEASE") ||
                                          !a.Name.Contains("DEBUG")));

                if (kextAsset is null)
                    kextAsset = targetRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));

                if (kextAsset is null)
                    return null;

                downloadUrl = kextAsset.DownloadUrl;
                fileName = kextAsset.Name;
            }
            else if (kext.DownloadInfo is not null)
            {
                downloadUrl = kext.DownloadInfo.Url;
                fileName = Path.GetFileName(downloadUrl);
            }
            else
            {
                // Try Dortania builds
                var dortaniaUrl = await GetDortaniaKextUrlAsync(kext.Name, ct);
                if (dortaniaUrl is null)
                    return null;

                downloadUrl = dortaniaUrl;
                fileName = $"{kext.Name}.zip";
            }

            if (string.IsNullOrEmpty(downloadUrl))
                return null;

            var outputPath = Path.Combine(outputDir, fileName);
            var success = await _download.DownloadFileAsync(downloadUrl, outputPath, ct: ct);

            if (!success)
                return null;

            // Extract and find .kext
            var extractDir = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(fileName));
            ZipFile.ExtractToDirectory(outputPath, extractDir, overwriteFiles: true);

            // Find .kext folder
            var kextPath = Directory.GetDirectories(extractDir, "*.kext", SearchOption.AllDirectories)
                .FirstOrDefault(d => Path.GetFileName(d) == $"{kext.Name}.kext");

            return kextPath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download kext {KextName}", kext.Name);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GatherRequiredKextsAsync(
        HardwareReport report,
        string targetMacos,
        string outputDir,
        CancellationToken ct = default)
    {
        var gatheredKexts = new Dictionary<string, string>();
        var requiredKexts = DetermineRequiredKexts(report, targetMacos);

        foreach (var kextName in requiredKexts)
        {
            var kextDef = KextData.Kexts.FirstOrDefault(k => k.Name == kextName);
            if (kextDef is not null)
            {
                var kextPath = await DownloadKextAsync(kextDef, outputDir, ct: ct);
                if (kextPath is not null)
                    gatheredKexts[kextName] = kextPath;
                else
                    _logger?.LogWarning("Failed to download required kext: {KextName}", kextName);
            }
        }

        return gatheredKexts;
    }

    public HashSet<string> DetermineRequiredKexts(HardwareReport report, string targetMacos)
    {
        var required = new HashSet<string>
        {
            // Always required
            "Lilu",
            "VirtualSMC"
        };

        // CPU-based kexts
        if (report.Cpu is { Manufacturer: "AuthenticAMD" })
        {
            required.Add("AMDRyzenCPUPowerManagement");
            required.Add("SMCAMDProcessor");
        }
        else if (report.Cpu is { Manufacturer: "GenuineIntel" })
        {
            required.Add("SMCProcessor");
        }

        // Platform-based
        if (report.Motherboard?.Platform == "Laptop")
        {
            required.Add("SMCBatteryManager");
            required.Add("VoodooPS2Controller");

            // Check for I2C trackpad
            if (HasI2CDevice(report))
            {
                required.Add("VoodooI2C");
                required.Add("VoodooI2CHID");
            }
        }
        else
        {
            required.Add("SMCSuperIO");
        }

        // GPU kexts
        if (report.Gpus is not null)
        {
            foreach (var (_, gpu) in report.Gpus)
            {
                if (gpu.Manufacturer.Contains("Intel") && gpu.DeviceType == "Integrated GPU")
                    required.Add("WhateverGreen");
                else if (gpu.Manufacturer.Contains("AMD") || gpu.Manufacturer.Contains("ATI"))
                    required.Add("WhateverGreen");
                else if (gpu.Manufacturer.Contains("NVIDIA"))
                    required.Add("WhateverGreen");
            }
        }

        // Audio
        if (report.Sound is not null && report.Sound.Count > 0)
            required.Add("AppleALC");

        // Network
        if (report.Network is not null)
        {
            foreach (var (_, net) in report.Network)
            {
                var deviceId = net.DeviceId.ToUpperInvariant();

                // Intel WiFi
                if (PciData.IntelWiFiIds.Any(id => deviceId.Contains(id)))
                {
                    required.Add("AirportItlwm");
                    required.Add("IntelBluetoothFirmware");
                    required.Add("IntelBTPatcher");
                }

                // Broadcom WiFi
                if (PciData.BroadcomWiFiIds.Any(id => deviceId.Contains(id)))
                {
                    required.Add("AirportBrcmFixup");
                }

                // Intel Ethernet
                if (PciData.IntelMausiIds.Any(id => deviceId.Contains(id)))
                    required.Add("IntelMausi");

                // Realtek Ethernet
                if (PciData.RealtekRtl8111Ids.Any(id => deviceId.Contains(id)))
                    required.Add("RealtekRTL8111");
            }
        }

        // NVMe
        if (report.StorageControllers?.Values.Any(s => s.BusType == "NVMe") == true)
            required.Add("NVMeFix");

        // USB
        required.Add("USBToolBox");

        // Add dependencies
        foreach (var kextName in required.ToList())
        {
            var kextDef = KextData.Kexts.FirstOrDefault(k => k.Name == kextName);
            if (kextDef?.RequiresKexts is not null)
            {
                foreach (var dep in kextDef.RequiresKexts)
                    required.Add(dep);
            }
        }

        return required;
    }

    private static bool HasI2CDevice(HardwareReport report)
    {
        // Check for I2C HID devices (touchpads, touchscreens)
        // This is a simplified check
        return report.Motherboard?.Platform == "Laptop";
    }

    private async Task<string?> GetDortaniaKextUrlAsync(string kextName, CancellationToken ct)
    {
        try
        {
            var manifestUrl = $"{DortaniaBuildsUrl}manifest.json";
            var manifestText = await _download.FetchTextAsync(manifestUrl, ct);
            if (manifestText is null)
                return null;

            var manifest = JsonDocument.Parse(manifestText);

            // Map kext names to package names
            var packageName = kextName switch
            {
                "Lilu" => "Lilu",
                "WhateverGreen" => "WhateverGreen",
                "AppleALC" => "AppleALC",
                "VirtualSMC" or "SMCProcessor" or "SMCSuperIO" or "SMCBatteryManager" => "VirtualSMC",
                "IntelMausi" => "IntelMausi",
                "AirportBrcmFixup" => "AirportBrcmFixup",
                _ => kextName
            };

            if (!manifest.RootElement.TryGetProperty(packageName, out var pkg))
                return null;

            if (!pkg.TryGetProperty("versions", out var versions))
                return null;

            if (versions.GetArrayLength() == 0)
                return null;

            var latest = versions[0];
            if (!latest.TryGetProperty("links", out var links))
                return null;

            if (!links.TryGetProperty("release", out var releaseUrl))
                return null;

            return releaseUrl.GetString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DownloadAcpiPatchAsync(
        string patchName,
        string outputDir,
        CancellationToken ct = default)
    {
        var patch = AcpiPatchData.Patches.FirstOrDefault(p => p.Name == patchName);
        if (patch is null)
            return false;

        try
        {
            // ACPI patches are typically .aml files from Acidanthera
            var url = $"{AcidicPluginsUrl}OpenCorePkg/master/Docs/AcpiSamples/Binaries/{patch.FunctionName}.aml";
            var outputPath = Path.Combine(outputDir, $"{patch.FunctionName}.aml");

            return await _download.DownloadFileAsync(url, outputPath, ct: ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download ACPI patch {PatchName}", patchName);
            return false;
        }
    }

    public async Task<bool> DownloadDriverAsync(
        string driverName,
        string outputDir,
        CancellationToken ct = default)
    {
        try
        {
            // Most drivers come with OpenCore package
            // For now, just return a placeholder path
            var driverPath = Path.Combine(outputDir, $"{driverName}.efi");

            // Common drivers: OpenRuntime.efi, HfsPlus.efi, OpenCanopy.efi
            var url = driverName switch
            {
                "HfsPlus" => $"{AcidicPluginsUrl}OcBinaryData/master/Drivers/HfsPlus.efi",
                _ => null
            };

            if (url is null)
            {
                _logger?.LogWarning("Driver {DriverName} should be extracted from OpenCore package", driverName);
                return false;
            }

            return await _download.DownloadFileAsync(url, driverPath, ct: ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download driver {DriverName}", driverName);
            return false;
        }
    }
}
