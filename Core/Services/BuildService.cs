using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using Claunia.PropertyList;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Datasets;
using OcsNet.Core.UsbMapper;

namespace OcsNet.Core.Services;

/// <summary>
/// Orchestrates the full EFI build: download → config → USB map → package.
/// </summary>
public sealed class BuildService
{
    private readonly DownloadService _download;
    private readonly GitHubService _github;
    private readonly ConfigService _config;
    private readonly UsbMapperService _usbMapper;
    private readonly AppUtils _utils;
    private readonly ILogger<BuildService>? _logger;

    public BuildService(
        DownloadService download,
        GitHubService github,
        ConfigService config,
        UsbMapperService usbMapper,
        AppUtils utils,
        ILogger<BuildService>? logger = null)
    {
        _download = download;
        _github   = github;
        _config   = config;
        _usbMapper = usbMapper;
        _utils    = utils;
        _logger   = logger;
    }

    /// <summary>
    /// Runs the full build.  Progress callbacks fire on the calling thread.
    /// Returns the path of the output zip on success; throws on failure.
    /// </summary>
    public async Task<string> BuildAsync(
        HardwareReport report,
        SmbiosData smbios,
        string macosVersion,
        HashSet<string> enabledKextNames,
        List<string> acpiPatchIds,
        List<UsbController> usbControllers,
        Func<string, int, string, Task> sendProgress,
        CancellationToken ct = default)
    {
        var workDir = _utils.GetTemporaryDir();
        var efiDir  = Path.Combine(workDir, "EFI");
        var ocDir   = Path.Combine(efiDir, "OC");

        try
        {
            // ── 1. Folder structure ──────────────────────────────────────────────
            await sendProgress("downloading-opencore", 2, "Creating EFI folder structure…");
            foreach (var sub in new[] { "BOOT", "OC/ACPI", "OC/Drivers", "OC/Kexts", "OC/Tools", "OC/Resources" })
                Directory.CreateDirectory(Path.Combine(efiDir, sub.Replace('/', Path.DirectorySeparatorChar)));

            // ── 2. Download + extract OpenCore ───────────────────────────────────
            await sendProgress("downloading-opencore", 5, "Fetching latest OpenCore release…");
            var ocExtracted = await TryDownloadOpenCoreAsync(workDir, efiDir, ocDir, ct);
            if (!ocExtracted)
                _logger?.LogWarning("OpenCore download failed — creating minimal EFI structure only");

            await sendProgress("downloading-opencore", 35, ocExtracted ? "OpenCore extracted" : "OpenCore unavailable — continuing");

            // ── 3. Download kexts ────────────────────────────────────────────────
            await sendProgress("downloading-kexts", 35, "Downloading kexts…");
            var kextsDlDir = Path.Combine(workDir, "_kexts_dl");
            Directory.CreateDirectory(kextsDlDir);
            await DownloadAndInstallKextsAsync(enabledKextNames, kextsDlDir, ocDir, ct,
                async (msg, pct) => await sendProgress("downloading-kexts", 35 + pct / 3, msg));
            await sendProgress("downloading-kexts", 65, "Kexts ready");

            // ── 4. Placeholder ACPI (config.plist handles patch names) ───────────
            await sendProgress("generating-acpi", 65, "Preparing ACPI section…");
            // No binary SSDTs generated here — the patches are referenced by name in config.plist.
            await Task.CompletedTask; // explicit async point

            // ── 5. config.plist ──────────────────────────────────────────────────
            await sendProgress("generating-config", 70, "Generating config.plist…");
            var configPlist = _config.GenerateConfig(report, smbios, macosVersion,
                enabledKextNames, acpiPatchIds);
            var configPath = Path.Combine(ocDir, "config.plist");
            await using (var fs = new FileStream(configPath, FileMode.Create))
                PropertyListParser.SaveAsXml(configPlist, fs);

            // ── 6. USB map kext ──────────────────────────────────────────────────
            await sendProgress("generating-usb-map", 80, "Generating USB map kext…");
            await GenerateUsbMapAsync(usbControllers, smbios, ocDir, ct);
            await sendProgress("generating-usb-map", 88, "USB map generated");

            // ── 7. Package ───────────────────────────────────────────────────────
            await sendProgress("packaging", 90, "Packaging output zip…");
            var outputPath = GetOutputZipPath();
            if (File.Exists(outputPath)) File.Delete(outputPath);
            ZipFile.CreateFromDirectory(workDir, outputPath);
            await sendProgress("complete", 100, $"Done → {outputPath}");

            return outputPath;
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    // ── OpenCore download + extraction ───────────────────────────────────────

    private async Task<bool> TryDownloadOpenCoreAsync(
        string workDir, string efiDir, string ocDir, CancellationToken ct)
    {
        try
        {
            var release = await _github.GetLatestReleaseAsync("acidanthera", "OpenCorePkg", ct);
            if (release is null) return false;

            // Pick the X64 RELEASE asset
            var asset = release.Assets.FirstOrDefault(a =>
                a.Name.Contains("RELEASE", StringComparison.OrdinalIgnoreCase) &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                !a.Name.Contains("DEBUG", StringComparison.OrdinalIgnoreCase));

            if (asset is null) return false;

            var zipPath = Path.Combine(workDir, "OpenCore.zip");
            if (!await _download.DownloadFileAsync(asset.DownloadUrl, zipPath, ct: ct))
                return false;

            // Extract into a temp subfolder, then copy X64/EFI content
            var extractDir = Path.Combine(workDir, "_oc_extract");
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            // Look for X64/EFI/…
            var x64Efi = Path.Combine(extractDir, "X64", "EFI");
            if (!Directory.Exists(x64Efi))
                x64Efi = Directory.GetDirectories(extractDir, "EFI", SearchOption.AllDirectories).FirstOrDefault()
                    ?? "";

            if (!Directory.Exists(x64Efi)) return false;

            // Copy BOOT and OC folders
            CopyDirectory(Path.Combine(x64Efi, "BOOT"), Path.Combine(efiDir, "BOOT"));
            CopyDirectory(Path.Combine(x64Efi, "OC"),   ocDir);

            // Clean up extracted OC/Kexts (we only want OC binary + drivers, not sample kexts)
            var kextsDir = Path.Combine(ocDir, "Kexts");
            if (Directory.Exists(kextsDir))
                foreach (var d in Directory.GetDirectories(kextsDir))
                    Directory.Delete(d, recursive: true);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "OpenCore download/extract failed");
            return false;
        }
    }

    // ── Kext download + extraction ────────────────────────────────────────────

    private async Task DownloadAndInstallKextsAsync(
        HashSet<string> kextNames, string dlDir, string ocDir,
        CancellationToken ct, Func<string, int, Task> onProgress)
    {
        var list   = kextNames.ToList();
        var kextsTarget = Path.Combine(ocDir, "Kexts");

        for (int i = 0; i < list.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var name = list[i];
            var pct  = (int)((double)i / list.Count * 100);
            await onProgress($"Downloading {name}…", pct);

            try
            {
                // Skip UTBMap — we generate it ourselves
                if (name.Equals("UTBMap", StringComparison.OrdinalIgnoreCase)) continue;

                var kext = KextData.Kexts.FirstOrDefault(k =>
                    k.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (kext is null) continue;

                // Try GitHub release
                if (kext.GithubRepo is not null)
                {
                    var releases = await _github.GetReleasesAsync(
                        kext.GithubRepo.Owner, kext.GithubRepo.Repo, ct);
                    var latest = releases.FirstOrDefault();
                    if (latest is null) continue;

                    var asset = latest.Assets.FirstOrDefault(a =>
                        a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                        !a.Name.Contains("dSYM", StringComparison.OrdinalIgnoreCase) &&
                        !a.Name.Contains("DEBUG", StringComparison.OrdinalIgnoreCase));
                    if (asset is null) continue;

                    var zipPath = Path.Combine(dlDir, $"{name}.zip");
                    if (!await _download.DownloadFileAsync(asset.DownloadUrl, zipPath, ct: ct))
                        continue;

                    ExtractKextFromZip(zipPath, kextsTarget, name);
                }
                else if (kext.DownloadInfo is not null)
                {
                    var zipPath = Path.Combine(dlDir, $"{name}.zip");
                    if (!await _download.DownloadFileAsync(kext.DownloadInfo.Url, zipPath, ct: ct))
                        continue;
                    ExtractKextFromZip(zipPath, kextsTarget, name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to download kext {Name}", name);
            }
        }
    }

    /// <summary>
    /// Extracts .kext bundles from a zip into the target directory.
    /// Handles both flat (Name.kext at root) and nested (Kexts/Name.kext) layouts.
    /// </summary>
    private static void ExtractKextFromZip(string zipPath, string targetDir, string kextName)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        // Collect unique .kext top-level paths in the archive
        var kextRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
        {
            // e.g. "Lilu.kext/Contents/Info.plist" → "Lilu.kext"
            //      "Kexts/Lilu.kext/Contents/…"    → "Kexts/Lilu.kext"
            var normalized = entry.FullName.Replace('\\', '/');
            var parts = normalized.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].EndsWith(".kext", StringComparison.OrdinalIgnoreCase))
                {
                    kextRoots.Add(string.Join("/", parts[..( i + 1)]));
                    break;
                }
            }
        }

        foreach (var kextRoot in kextRoots)
        {
            var kextDirName = Path.GetFileName(kextRoot);
            var destKext    = Path.Combine(targetDir, kextDirName);
            if (Directory.Exists(destKext)) Directory.Delete(destKext, true);

            foreach (var entry in zip.Entries)
            {
                var normalized = entry.FullName.Replace('\\', '/');
                if (!normalized.StartsWith(kextRoot + "/", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relative = normalized[(kextRoot.Length + 1)..];
                if (string.IsNullOrEmpty(relative)) continue;

                var destPath = Path.Combine(destKext, relative.Replace('/', Path.DirectorySeparatorChar));
                if (entry.FullName.EndsWith('/'))
                {
                    Directory.CreateDirectory(destPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }
        }
    }

    // ── USB map kext generation ───────────────────────────────────────────────

    private async Task GenerateUsbMapAsync(
        List<UsbController> controllers, SmbiosData smbios, string ocDir, CancellationToken ct)
    {
        if (controllers.Count == 0 || !controllers.Any(c => c.Ports.Any(p => p.Selected)))
        {
            _logger?.LogInformation("No USB ports selected — skipping UTBMap.kext generation");
            return;
        }

        // Push the selected controllers into UsbMapperService history so it can build the kext
        foreach (var controller in controllers)
            controller.SelectedCount = controller.Ports.Count(p => p.Selected);

        var kextsDir = Path.Combine(ocDir, "Kexts");
        var kextPath = await _usbMapper.BuildKextAsync(kextsDir, smbios.SystemProductName, ct);
        if (kextPath is not null)
            _logger?.LogInformation("UTBMap.kext written to {Path}", kextPath);
        else
            _logger?.LogWarning("UTBMap.kext generation failed");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetOutputZipPath()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var stamp   = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(desktop, $"OCS-EFI-{stamp}.zip");
    }

    private static void CopyDirectory(string src, string dst)
    {
        if (!Directory.Exists(src)) return;
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var rel  = Path.GetRelativePath(src, file);
            var dest = Path.Combine(dst, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
