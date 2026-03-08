using OcsNet.Core.Datasets;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.Services;

/// <summary>
/// Service for hardware customization — filtering devices by compatibility,
/// handling multi-device selection (GPU, WiFi, Bluetooth).
/// Ported from hardware_customizer.py.
/// </summary>
public sealed class HardwareCustomizerService
{
    private readonly CompatibilityService _compatibility;
    private readonly ILogger<HardwareCustomizerService>? _logger;

    public HardwareCustomizerService(
        CompatibilityService compatibility,
        ILogger<HardwareCustomizerService>? logger = null)
    {
        _compatibility = compatibility;
        _logger = logger;
    }

    /// <summary>
    /// Filters hardware devices by macOS compatibility and returns
    /// customized hardware, disabled devices, and whether OCLP is needed. 
    /// </summary>
    public HardwareCustomizationResult Customize(HardwareReport report, string macosVersion)
    {
        var result = new HardwareCustomizationResult();
        var darwin = OsData.ParseDarwinVersion(macosVersion);

        // Filter GPUs
        if (report.Gpus is not null)
        {
            foreach (var (name, gpu) in report.Gpus)
            {
                var compat = _compatibility.CheckGpuCompatibility(gpu);
                if (compat is null || compat.Value.Min is null)
                {
                    result.DisabledDevices.Add(new DisabledDevice("GPU", name, gpu.DeviceId, "Not compatible with macOS"));
                    continue;
                }

                var minDarwin = OsData.ParseDarwinVersion(compat.Value.Min);
                var maxDarwin = OsData.ParseDarwinVersion(compat.Value.Max ?? OsData.GetLatestDarwinVersion());

                if (darwin.Major >= minDarwin.Major && darwin.Major <= maxDarwin.Major)
                {
                    result.CompatibleGpus.Add(new DeviceOption(name, gpu.DeviceId, gpu.DeviceType, true));
                }
                else
                {
                    result.DisabledDevices.Add(new DisabledDevice("GPU", name, gpu.DeviceId,
                        $"Not compatible with {macosVersion}"));
                }
            }
        }

        // Filter Network (WiFi only for selection)
        if (report.Network is not null)
        {
            foreach (var (name, net) in report.Network)
            {
                var deviceId = net.DeviceId.ToUpperInvariant();
                var isWifi = PciData.WirelessCardIds.Any(id => deviceId.Contains(id));

                if (!isWifi) continue;

                var compat = _compatibility.CheckNetworkCompatibility(net);
                if (compat?.Min is not null)
                {
                    result.CompatibleWifi.Add(new DeviceOption(name, net.DeviceId, "WiFi", true));
                }
                else
                {
                    result.DisabledDevices.Add(new DisabledDevice("WiFi", name, net.DeviceId,
                        "Not natively supported"));
                }
            }
        }

        // Detect if OCLP needed
        result.NeedsOclp = result.DisabledDevices.Any(d =>
            d.Category == "GPU" && report.Gpus?.ContainsKey(d.Name) == true);

        // Check multiple GPUs
        result.HasMultipleGpus = result.CompatibleGpus.Count > 1;
        result.HasMultipleWifi = result.CompatibleWifi.Count > 1;

        // Handle APU + dGPU conflicts
        if (result.HasMultipleGpus)
        {
            var hasAmdApu = result.CompatibleGpus.Any(g => g.DeviceType == "Integrated GPU");
            var hasNavi22 = report.Gpus?.Values.Any(g => g.Codename == "Navi 22") == true;

            if (hasAmdApu || hasNavi22)
            {
                result.GpuConflictWarning = "Multiple active GPUs detected. " +
                    "Some GPU combinations can cause kext conflicts in macOS. " +
                    "Consider disabling one GPU.";
            }
        }

        return result;
    }

    /// <summary>
    /// Applies user's device selection, returning an updated report with disabled devices removed.
    /// </summary>
    public HardwareReport ApplySelection(
        HardwareReport report,
        List<string>? selectedGpuNames,
        List<string>? selectedWifiNames)
    {
        if (selectedGpuNames is not null && report.Gpus is not null)
        {
            var toRemove = report.Gpus.Keys
                .Where(k => !selectedGpuNames.Contains(k))
                .ToList();
            foreach (var key in toRemove)
                report.Gpus.Remove(key);
        }

        if (selectedWifiNames is not null && report.Network is not null)
        {
            var wifiIds = PciData.WirelessCardIds;
            var toRemove = report.Network
                .Where(kv => wifiIds.Any(id => kv.Value.DeviceId.ToUpperInvariant().Contains(id))
                              && !selectedWifiNames.Contains(kv.Key))
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in toRemove)
                report.Network.Remove(key);
        }

        return report;
    }
}

public class HardwareCustomizationResult
{
    public List<DeviceOption> CompatibleGpus { get; set; } = [];
    public List<DeviceOption> CompatibleWifi { get; set; } = [];
    public List<DisabledDevice> DisabledDevices { get; set; } = [];
    public bool NeedsOclp { get; set; }
    public bool HasMultipleGpus { get; set; }
    public bool HasMultipleWifi { get; set; }
    public string? GpuConflictWarning { get; set; }
}

public record DeviceOption(string Name, string DeviceId, string DeviceType, bool Selected);
public record DisabledDevice(string Category, string Name, string DeviceId, string Reason);
