using System.Runtime.InteropServices;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.Services;

/// <summary>
/// Service for DSDT/SSDT ACPI table handling.
/// Wraps iasl binary for disassembly and compilation.
/// </summary>
public sealed class DsdtService
{
    private readonly ProcessRunner _process;
    private readonly DownloadService _download;
    private readonly GitHubService _github;
    private readonly AppUtils _utils;
    private readonly ILogger<DsdtService>? _logger;

    private const string IaslUrlMacOS = "https://raw.githubusercontent.com/acidanthera/MaciASL/master/Dist/iasl-stable";
    private const string IaslUrlLinux = "https://raw.githubusercontent.com/corpnewt/linux_iasl/main/iasl.zip";
    private const string AcpicaReleases = "https://github.com/acpica/acpica/releases";

    private readonly HashSet<string> _allowedSignatures = ["APIC", "DMAR", "DSDT", "SSDT"];
    private string? _iaslPath;

    public DsdtService(
        ProcessRunner process,
        DownloadService download,
        GitHubService github,
        AppUtils utils,
        ILogger<DsdtService>? logger = null)
    {
        _process = process;
        _download = download;
        _github = github;
        _utils = utils;
        _logger = logger;
    }

    public async Task<bool> EnsureIaslAvailableAsync(CancellationToken ct = default)
    {
        _iaslPath = FindIasl();
        if (_iaslPath is not null)
            return true;

        return await DownloadIaslAsync(ct);
    }

    public string? FindIasl()
    {
        var scriptDir = AppContext.BaseDirectory;
        string[] candidates;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            candidates = ["iasl.exe", "iasl-stable.exe"];
        else
            candidates = ["iasl", "iasl-stable", "iasl-dev"];

        foreach (var binary in candidates)
        {
            var path = Path.Combine(scriptDir, binary);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private async Task<bool> DownloadIaslAsync(CancellationToken ct)
    {
        var scriptDir = AppContext.BaseDirectory;
        var tempDir = Path.Combine(Path.GetTempPath(), $"iasl_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            string downloadUrl;
            string targetName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Get latest from GitHub releases
                var releases = await _github.GetReleasesAsync("acpica", "acpica", ct);
                var latestRelease = releases.FirstOrDefault();
                var asset = latestRelease?.Assets.FirstOrDefault(a =>
                    a.Name.Contains("iasl") && a.Name.EndsWith(".zip"));

                if (asset is null)
                {
                    _logger?.LogError("Could not find iasl download for Windows");
                    return false;
                }

                downloadUrl = asset.DownloadUrl;
                targetName = "iasl.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                downloadUrl = IaslUrlMacOS;
                targetName = "iasl-stable";
            }
            else
            {
                downloadUrl = IaslUrlLinux;
                targetName = "iasl";
            }

            var downloadPath = Path.Combine(tempDir, Path.GetFileName(downloadUrl));
            var success = await _download.DownloadFileAsync(downloadUrl, downloadPath, ct: ct);
            if (!success)
                return false;

            // Extract if ZIP
            if (downloadPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var extractDir = Path.Combine(tempDir, "extracted");
                ZipFile.ExtractToDirectory(downloadPath, extractDir);

                // Find iasl binary
                var iaslFile = Directory.GetFiles(extractDir, "iasl*", SearchOption.AllDirectories)
                    .FirstOrDefault(f => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? f.EndsWith(".exe")
                        : !f.EndsWith(".exe"));

                if (iaslFile is null)
                    return false;

                var destPath = Path.Combine(scriptDir, targetName);
                File.Copy(iaslFile, destPath, overwrite: true);
                _iaslPath = destPath;
            }
            else
            {
                // Direct binary download (macOS)
                var destPath = Path.Combine(scriptDir, targetName);
                File.Copy(downloadPath, destPath, overwrite: true);

                // Make executable on Unix
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    await _process.RunAsync("chmod", ["+x", destPath], cancellationToken: ct);

                _iaslPath = destPath;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download iasl");
            return false;
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    public async Task<AcpiTable?> LoadTableAsync(string tablePath, CancellationToken ct = default)
    {
        if (!File.Exists(tablePath))
            return null;

        // Read raw bytes and verify signature
        var rawBytes = await File.ReadAllBytesAsync(tablePath, ct);
        if (rawBytes.Length < 28)
            return null;

        var signature = System.Text.Encoding.ASCII.GetString(rawBytes[..4]);
        if (!_allowedSignatures.Contains(signature))
            return null;

        var table = new AcpiTable
        {
            RawBytes = rawBytes,
            Signature = signature,
            Length = BitConverter.ToUInt32(rawBytes[4..8]),
            Revision = rawBytes[8],
            Checksum = rawBytes[9],
            OemId = System.Text.Encoding.ASCII.GetString(rawBytes[10..16]).TrimEnd('\0'),
            TableId = System.Text.Encoding.ASCII.GetString(rawBytes[16..24]).TrimEnd('\0'),
            OemRevision = BitConverter.ToUInt32(rawBytes[24..28]),
            FilePath = tablePath
        };

        // Disassemble to DSL
        if (_iaslPath is not null)
        {
            var dslContent = await DisassembleAsync(tablePath, ct);
            if (dslContent is not null)
            {
                table.DslContent = dslContent;
                table.Scopes = ExtractScopes(dslContent);
                table.Paths = ExtractPaths(dslContent);
            }
        }

        return table;
    }

    public async Task<string?> DisassembleAsync(string amlPath, CancellationToken ct = default)
    {
        if (_iaslPath is null)
            return null;

        var tempDir = Path.Combine(Path.GetTempPath(), $"dsdt_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            var fileName = Path.GetFileName(amlPath);
            var tempAml = Path.Combine(tempDir, fileName);
            File.Copy(amlPath, tempAml);

            // Run iasl for disassembly
            var result = await _process.RunAsync(
                _iaslPath,
                ["-da", "-dl", "-l", fileName],
                tempDir,
                cancellationToken: ct);

            if (!result.Success)
            {
                // Try without -da flag
                result = await _process.RunAsync(
                    _iaslPath,
                    ["-dl", "-l", fileName],
                    tempDir,
                    cancellationToken: ct);
            }

            var dslPath = Path.Combine(tempDir, Path.ChangeExtension(fileName, ".dsl"));
            if (!File.Exists(dslPath))
                return null;

            var content = await File.ReadAllTextAsync(dslPath, ct);

            // Remove compiler info header
            if (content.StartsWith("/*"))
            {
                var endComment = content.IndexOf("*/", StringComparison.Ordinal);
                if (endComment > 0)
                    content = content[(endComment + 2)..].TrimStart();
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to disassemble {Path}", amlPath);
            return null;
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    public async Task<byte[]?> CompileAsync(string dslContent, CancellationToken ct = default)
    {
        if (_iaslPath is null)
            return null;

        var tempDir = Path.Combine(Path.GetTempPath(), $"dsdt_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            var dslPath = Path.Combine(tempDir, "table.dsl");
            await File.WriteAllTextAsync(dslPath, dslContent, ct);

            var result = await _process.RunAsync(
                _iaslPath,
                ["table.dsl"],
                tempDir,
                cancellationToken: ct);

            var amlPath = Path.Combine(tempDir, "table.aml");
            if (!File.Exists(amlPath))
                return null;

            return await File.ReadAllBytesAsync(amlPath, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to compile DSL");
            return null;
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static List<AcpiScope> ExtractScopes(string dslContent)
    {
        var scopes = new List<AcpiScope>();
        var typeMatch = new System.Text.RegularExpressions.Regex(
            @"(?<type>Processor|Scope|Device|Method|Name)\s*\((?<name>[^,\)]+)");

        foreach (System.Text.RegularExpressions.Match match in typeMatch.Matches(dslContent))
        {
            scopes.Add(new AcpiScope
            {
                Type = match.Groups["type"].Value,
                Name = match.Groups["name"].Value.Trim()
            });
        }

        return scopes;
    }

    private static List<string> ExtractPaths(string dslContent)
    {
        var paths = new HashSet<string>();

        // Extract ACPI paths like \_SB.PCI0.LPCB
        var pathMatch = new System.Text.RegularExpressions.Regex(@"\\[_A-Z0-9\.]+");
        foreach (System.Text.RegularExpressions.Match match in pathMatch.Matches(dslContent))
        {
            paths.Add(match.Value);
        }

        return [.. paths.OrderBy(p => p)];
    }

    public bool ValidateChecksum(byte[] tableBytes)
    {
        byte sum = 0;
        foreach (var b in tableBytes)
            sum += b;
        return sum == 0;
    }
}

public sealed class AcpiTable
{
    public byte[] RawBytes { get; set; } = [];
    public string Signature { get; set; } = "";
    public uint Length { get; set; }
    public byte Revision { get; set; }
    public byte Checksum { get; set; }
    public string OemId { get; set; } = "";
    public string TableId { get; set; } = "";
    public uint OemRevision { get; set; }
    public string FilePath { get; set; } = "";
    public string? DslContent { get; set; }
    public List<AcpiScope> Scopes { get; set; } = [];
    public List<string> Paths { get; set; } = [];
}

public sealed class AcpiScope
{
    public string Type { get; set; } = ""; // Scope, Device, Method, Name, Processor
    public string Name { get; set; } = "";
}
