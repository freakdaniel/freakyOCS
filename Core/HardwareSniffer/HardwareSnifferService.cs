using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Datasets;
using OcsNet.Core.Services;

namespace OcsNet.Core.HardwareSniffer;

/// <summary>
/// Cross-platform hardware information collector.
/// Windows: Uses WMI and SetupAPI
/// Linux: Uses lspci, lsusb, and /sys filesystem
/// macOS: Uses system_profiler and ioreg
/// </summary>
public sealed partial class HardwareSnifferService
{
    private readonly ILogger<HardwareSnifferService>? _logger;
    private readonly CompatibilityService _compatibility;

    public HardwareSnifferService(CompatibilityService compatibility, ILogger<HardwareSnifferService>? logger = null)
    {
        _compatibility = compatibility;
        _logger = logger;
    }

    public async Task<HardwareReport> CollectHardwareAsync(CancellationToken ct = default)
    {
        var report = new HardwareReport();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await CollectWindowsHardwareAsync(report, ct);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await CollectLinuxHardwareAsync(report, ct);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await CollectMacOSHardwareAsync(report, ct);
            }

            // Apply compatibility analysis
            ApplyCompatibilityInfo(report);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to collect hardware information");
        }

        return report;
    }

    private void ApplyCompatibilityInfo(HardwareReport report)
    {
        if (report.Cpu is not null)
        {
            report.Cpu.Codename = CpuData.GetCodename(report.Cpu.ProcessorName, report.Cpu.Manufacturer);
        }

        if (report.Gpus is not null)
        {
            foreach (var (_, gpu) in report.Gpus)
            {
                gpu.Codename = GpuData.GetCodename(gpu.DeviceId, gpu.Manufacturer);
                gpu.Compatibility = _compatibility.CheckGpuCompatibility(gpu);
            }
        }

        if (report.Sound is not null)
        {
            foreach (var (_, audio) in report.Sound)
            {
                audio.Compatibility = _compatibility.CheckAudioCompatibility(audio);
            }
        }

        if (report.Network is not null)
        {
            foreach (var (_, net) in report.Network)
            {
                net.Compatibility = _compatibility.CheckNetworkCompatibility(net);
            }
        }

        if (report.Bluetooth is not null)
        {
            foreach (var (_, bt) in report.Bluetooth)
            {
                bt.Compatibility = _compatibility.CheckBluetoothCompatibility(bt);
            }
        }
    }

    public async Task ExportReportAsync(HardwareReport report, string outputPath, CancellationToken ct = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Export the frontend DTO so the saved file can be re-imported via drag & drop
        var dto = OcsNet.Core.Bridge.HardwareFrontendReport.From(report);
        var json = JsonSerializer.Serialize(dto, options);
        await File.WriteAllTextAsync(outputPath, json, ct);
    }

    public async Task<HardwareReport?> ImportReportAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath, ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<HardwareReport>(json, options);
    }

    private async Task<string> RunCommandAsync(string command, string args, CancellationToken ct)
    {
        try
        {
            using var process = new Process();

            // Inject UTF-8 console encoding preamble into PowerShell commands so that
            // non-ASCII device names (e.g. Russian locale) are not mangled.
            var isPs = Path.GetFileNameWithoutExtension(command)
                .Equals("powershell", StringComparison.OrdinalIgnoreCase);
            var finalArgs = isPs && args.Contains("-Command \"")
                ? args.Replace("-Command \"", "-Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; ")
                : args;

            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = finalArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };

            // Per-command timeout: lspci/lsusb should never take more than 8 s.
            // Without this a hung process blocks the whole hardware detection
            // and triggers the 30 s bridge timeout on the frontend.
            using var cmdCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cmdCts.CancelAfter(TimeSpan.FromSeconds(8));

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cmdCts.Token);
            await process.WaitForExitAsync(cmdCts.Token);
            return output;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Command {Command} timed out", command);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Command {Command} failed: {Error}", command, ex.Message);
            return string.Empty;
        }
    }

    [GeneratedRegex(@"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PciIdRegex();

    [GeneratedRegex(@"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase)]
    private static partial Regex UsbIdRegex();

    [GeneratedRegex(@"([0-9a-f]{4}):([0-9a-f]{4})", RegexOptions.IgnoreCase)]
    private static partial Regex LinuxPciIdRegex();
}
