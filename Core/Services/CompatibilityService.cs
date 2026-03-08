using OcsNet.Core.Datasets;

namespace OcsNet.Core.Services;

/// <summary>
/// Service for checking hardware compatibility with macOS versions.
/// Returns Darwin version ranges (min, max) for each hardware component.
/// </summary>
public sealed class CompatibilityService
{
    // Darwin version limits
    private static readonly string MaxDarwin = "99.0.0";
    private static readonly string MinDarwin = "0.0.0";
    private static readonly string SequoiaMin = "24.0.0"; // macOS Sequoia (15) minimum

    public (string? Max, string? Min)? CheckCpuCompatibility(CpuInfo cpu, string targetMacos)
    {
        var (darwinMajor, _, _) = OsData.ParseDarwinVersion(targetMacos);

        if (cpu.Manufacturer == "GenuineIntel")
            return CheckIntelCpuCompatibility(cpu, darwinMajor);

        if (cpu.Manufacturer == "AuthenticAMD")
            return CheckAmdCpuCompatibility(cpu, darwinMajor);

        return (null, null); // Unsupported manufacturer
    }

    private (string? Max, string? Min)? CheckIntelCpuCompatibility(CpuInfo cpu, int darwinMajor)
    {
        var codename = cpu.Codename;

        // Modern Intel (Kaby Lake+) - full support
        if (IsModernIntel(codename))
            return (MaxDarwin, "17.0.0"); // macOS High Sierra+

        // Skylake
        if (codename.Contains("Skylake"))
            return (MaxDarwin, "15.0.0"); // El Capitan+

        // Broadwell
        if (codename.Contains("Broadwell"))
            return (MaxDarwin, "14.0.0"); // Yosemite+

        // Haswell
        if (codename.Contains("Haswell"))
            return (MaxDarwin, "13.0.0"); // Mavericks+

        // Ivy Bridge - dropped in Ventura
        if (codename.Contains("Ivy Bridge"))
        {
            if (!cpu.SimdFeatures.Contains("AVX"))
                return (null, null);
            return ("21.99.99", "12.0.0"); // Mountain Lion through Monterey
        }

        // Sandy Bridge - dropped in Ventura
        if (codename.Contains("Sandy Bridge"))
        {
            if (!cpu.SimdFeatures.Contains("AVX"))
                return (null, null);
            return ("21.99.99", "11.0.0"); // Lion through Monterey
        }

        // Legacy Intel
        if (IsLegacyIntel(codename))
            return CheckLegacyIntelCompatibility(codename);

        // Unsupported/unknown
        return (null, null);
    }

    private static bool IsModernIntel(string codename) =>
        codename.Contains("Kaby Lake") || codename.Contains("Coffee Lake") ||
        codename.Contains("Cannon Lake") || codename.Contains("Whiskey Lake") ||
        codename.Contains("Amber Lake") || codename.Contains("Comet Lake") ||
        codename.Contains("Ice Lake") || codename.Contains("Tiger Lake") ||
        codename.Contains("Alder Lake") || codename.Contains("Raptor Lake") ||
        codename.Contains("Meteor Lake") || codename.Contains("Arrow Lake");

    private static bool IsLegacyIntel(string codename) =>
        codename is "Westmere" or "Clarkdale" or "Lynnfield" or "Nehalem"
            or "Penryn" or "Core 2" or "Conroe" or "Merom" or "Yonah";

    private static (string? Max, string? Min)? CheckLegacyIntelCompatibility(string codename) =>
        codename switch
        {
            "Westmere" or "Clarkdale" or "Lynnfield" => ("18.99.99", "10.0.0"), // High Sierra max
            "Nehalem" => ("17.99.99", "9.0.0"), // Sierra max
            "Penryn" or "Core 2" => ("17.99.99", "8.0.0"), // Sierra max
            "Conroe" or "Merom" or "Yonah" => ("16.99.99", "7.0.0"), // El Capitan max
            _ => (null, null)
        };

    private (string? Max, string? Min)? CheckAmdCpuCompatibility(CpuInfo cpu, int darwinMajor)
    {
        var codename = cpu.Codename;

        // AMD requires SSE4.2 minimum
        if (!cpu.SimdFeatures.Contains("SSE4.2"))
            return (null, null);

        // Zen architecture (Ryzen, Threadripper, EPYC)
        if (IsAmdZen(codename))
            return (MaxDarwin, "17.0.0"); // High Sierra+

        // Bulldozer family - limited support
        if (IsAmdBulldozer(codename))
            return ("20.6.0", "17.0.0"); // Big Sur max with High Sierra min

        // FX/K10 - very limited
        if (codename.Contains("K10") || codename.Contains("Phenom"))
            return ("18.99.99", "10.0.0"); // Mojave max

        return (null, null);
    }

    private static bool IsAmdZen(string codename) =>
        codename.Contains("Zen") || codename.Contains("Ryzen") ||
        codename.Contains("Threadripper") || codename.Contains("EPYC") ||
        codename.Contains("Matisse") || codename.Contains("Vermeer") ||
        codename.Contains("Raphael") || codename.Contains("Granite Ridge");

    private static bool IsAmdBulldozer(string codename) =>
        codename.Contains("Bulldozer") || codename.Contains("Piledriver") ||
        codename.Contains("Steamroller") || codename.Contains("Excavator");

    public (string? Max, string? Min)? CheckGpuCompatibility(GpuInfo gpu)
    {
        var codename = gpu.Codename;
        var deviceId = gpu.DeviceId.ToUpperInvariant();
        var manufacturer = gpu.Manufacturer;

        // Intel iGPU
        if (manufacturer.Contains("Intel"))
            return CheckIntelGpuCompatibility(codename, deviceId);

        // AMD dGPU
        if (manufacturer.Contains("AMD") || manufacturer.Contains("ATI"))
            return CheckAmdGpuCompatibility(codename, deviceId);

        // NVIDIA - very limited support
        if (manufacturer.Contains("NVIDIA"))
            return CheckNvidiaGpuCompatibility(codename);

        return (null, null);
    }

    private static (string? Max, string? Min)? CheckIntelGpuCompatibility(string codename, string deviceId)
    {
        // Ice Lake+ iGPU
        if (codename.Contains("Ice Lake") || codename.Contains("Tiger Lake"))
            return (MaxDarwin, "19.0.0"); // Catalina+

        // Coffee Lake/Comet Lake UHD 630
        if (codename.Contains("Coffee Lake") || codename.Contains("Comet Lake"))
            return (MaxDarwin, "17.0.0"); // High Sierra+

        // Kaby Lake
        if (codename.Contains("Kaby Lake") || codename.Contains("Amber Lake"))
            return (MaxDarwin, "16.0.0"); // Sierra+

        // Skylake
        if (codename.Contains("Skylake"))
            return (MaxDarwin, "15.0.0"); // El Capitan+

        // Broadwell
        if (codename.Contains("Broadwell"))
            return (MaxDarwin, "14.0.0"); // Yosemite+

        // Haswell
        if (codename.Contains("Haswell"))
            return ("23.99.99", "13.0.0"); // Sonoma max, Mavericks+

        // Ivy Bridge - dropped in Monterey  
        if (codename.Contains("Ivy Bridge"))
            return ("20.6.0", "12.0.0"); // Big Sur max

        // Sandy Bridge - dropped
        if (codename.Contains("Sandy Bridge"))
            return ("18.99.99", "11.0.0"); // Mojave max

        // Alder Lake+ has no iGPU support
        if (codename.Contains("Alder Lake") || codename.Contains("Raptor Lake") ||
            codename.Contains("Meteor Lake") || codename.Contains("Arrow Lake"))
            return (null, null);

        return (null, null);
    }

    private static (string? Max, string? Min)? CheckAmdGpuCompatibility(string codename, string deviceId)
    {
        // RDNA 3 - Navi 3x
        if (codename.Contains("Navi 3"))
            return (MaxDarwin, SequoiaMin); // Sequoia+

        // RDNA 2 - Navi 2x
        if (codename.Contains("Navi 2"))
            return (MaxDarwin, "20.4.0"); // Big Sur 11.4+

        // RDNA 1 - Navi 1x
        if (codename.Contains("Navi 1") || codename == "Navi")
            return (MaxDarwin, "19.0.0"); // Catalina+

        // Vega
        if (codename.Contains("Vega"))
            return (MaxDarwin, "17.0.0"); // High Sierra+

        // Polaris
        if (codename.Contains("Polaris") || codename.Contains("Ellesmere") ||
            codename.Contains("Baffin") || codename.Contains("Lexa"))
            return (MaxDarwin, "16.0.0"); // Sierra+

        // GCN 3/4 (Hawaii, Tonga, Fiji)
        if (codename is "Hawaii" or "Tonga" or "Fiji" or "Antigua" or "Grenada")
            return ("23.99.99", "14.0.0"); // Sonoma max, Yosemite+

        // GCN 1/2 - dropped in Monterey
        if (IsLegacyGcn(codename))
            return ("20.6.0", "12.0.0"); // Big Sur max

        return (null, null);
    }

    private static bool IsLegacyGcn(string codename) =>
        codename is "Tahiti" or "Pitcairn" or "Verde" or "Oland" or "Cape Verde" or "Bonaire";

    private static (string? Max, string? Min)? CheckNvidiaGpuCompatibility(string codename)
    {
        // Kepler - last supported NVIDIA architecture (via Web Drivers OR natively)
        if (codename.Contains("Kepler") || codename.Contains("GK"))
            return ("17.99.99", "12.0.0"); // High Sierra max with native, Mountain Lion+

        // Maxwell/Pascal/Turing/Ampere - Web Drivers only through High Sierra
        if (codename.Contains("Maxwell") || codename.Contains("GM") ||
            codename.Contains("Pascal") || codename.Contains("GP") ||
            codename.Contains("Turing") || codename.Contains("TU") ||
            codename.Contains("Ampere") || codename.Contains("GA"))
            return ("17.99.99", "13.0.0"); // High Sierra max (needs web drivers)

        // Ada Lovelace and newer - no support
        if (codename.Contains("Ada") || codename.Contains("AD") ||
            codename.Contains("Hopper") || codename.Contains("Blackwell"))
            return (null, null);

        // Older NVIDIA (Fermi, Tesla) - El Capitan max
        return ("15.99.99", "10.0.0");
    }

    public (string? Max, string? Min)? CheckAudioCompatibility(AudioInfo audio)
    {
        var deviceId = audio.DeviceId.ToUpperInvariant();

        // USB Audio - generally always works
        if (audio.BusType == "USB")
            return (MaxDarwin, "10.0.0");

        // Check codec layouts
        if (CodecLayouts.Data.TryGetValue(deviceId, out _))
            return (MaxDarwin, "13.0.0"); // AppleALC support starting Mavericks

        // HDMI/DP audio typically works with GPU
        if (deviceId.Contains("HDMI") || deviceId.Contains("DISPLAY"))
            return (MaxDarwin, "13.0.0");

        return (null, null);
    }

    public (string? Max, string? Min)? CheckNetworkCompatibility(NetworkInfo network)
    {
        var deviceId = network.DeviceId.ToUpperInvariant();

        // USB Ethernet/WiFi adapters
        if (network.BusType == "USB")
            return (MaxDarwin, "12.0.0"); // Generally work with kexts

        // Intel Ethernet - check against known IDs
        if (PciData.IntelMausiIds.Any(id => deviceId.Contains(id)))
            return (MaxDarwin, "13.0.0"); // IntelMausi support

        // Realtek Ethernet
        if (PciData.RealtekRtl8111Ids.Any(id => deviceId.Contains(id)))
            return (MaxDarwin, "13.0.0"); // RealtekRTL8111 support

        // Intel WiFi
        if (PciData.IntelWiFiIds.Any(id => deviceId.Contains(id)))
            return (MaxDarwin, "19.0.0"); // AirportItlwm (Catalina+)

        // Broadcom WiFi
        if (PciData.BroadcomWiFiIds.Any(id => deviceId.Contains(id)))
            return (MaxDarwin, "13.0.0"); // Native or AirportBrcmFixup

        // Unsupported WiFi (Realtek, Mediatek, Qualcomm native)
        return (null, null);
    }

    public (string? Max, string? Min)? CheckStorageCompatibility(StorageInfo storage)
    {
        var deviceId = storage.DeviceId.ToUpperInvariant();

        // SATA - always works
        if (storage.BusType == "SATA" || storage.BusType == "AHCI")
            return (MaxDarwin, "10.0.0");

        // NVMe - check against problematic controllers
        if (storage.BusType == "NVMe")
        {
            // Samsung PM981/PM991 and Intel 600P need NVMeFix
            // Most NVMe works from High Sierra+
            return (MaxDarwin, "17.0.0");
        }

        // USB storage
        if (storage.BusType == "USB")
            return (MaxDarwin, "10.0.0");

        return (MaxDarwin, "12.0.0"); // Default assumption
    }

    public (string? Max, string? Min)? CheckBluetoothCompatibility(BluetoothInfo bluetooth)
    {
        var deviceId = bluetooth.DeviceId.ToUpperInvariant();

        // Intel Bluetooth
        if (PciData.IntelBluetoothIds.Any(id => deviceId.Contains(id)))
            return (MaxDarwin, "19.0.0"); // IntelBluetoothFirmware (Catalina+)

        // Broadcom Bluetooth - native or with kext
        if (PciData.BroadcomBluetoothIds.Any(id => deviceId.Contains(id)))
            return (MaxDarwin, "13.0.0");

        return (null, null);
    }

    public (string? Max, string? Min)? CheckBiometricCompatibility(BiometricInfo biometric)
    {
        // Fingerprint readers - never supported
        if (biometric.DeviceType.Contains("Fingerprint"))
            return (null, null);

        // IR cameras - limited support
        if (biometric.DeviceType.Contains("IR Camera"))
            return (null, null);

        return (null, null);
    }

    /// <summary>
    /// Calculates the overall system compatibility range by finding
    /// the intersection of all component ranges.
    /// </summary>
    public (string? Max, string? Min)? CalculateOverallCompatibility(HardwareReport report)
    {
        var ranges = new List<(string Max, string Min)>();

        if (report.Cpu is not null)
        {
            var cpuCompat = CheckCpuCompatibility(report.Cpu, "");
            if (cpuCompat?.Max is not null && cpuCompat?.Min is not null)
                ranges.Add((cpuCompat.Value.Max!, cpuCompat.Value.Min!));
            else
                return (null, null); // CPU must be compatible
        }

        if (report.Gpus is not null)
        {
            foreach (var (_, gpu) in report.Gpus)
            {
                var gpuCompat = CheckGpuCompatibility(gpu);
                if (gpuCompat?.Max is not null && gpuCompat?.Min is not null)
                    ranges.Add((gpuCompat.Value.Max!, gpuCompat.Value.Min!));
            }
        }

        if (ranges.Count == 0)
            return (null, null);

        // Find intersection
        var maxDarwin = ranges.Min(r => OsData.ParseDarwinVersion(r.Max));
        var minDarwin = ranges.Max(r => OsData.ParseDarwinVersion(r.Min));

        // Check if range is valid
        if (OsData.CompareDarwinVersions($"{maxDarwin.Major}.{maxDarwin.Minor}.{maxDarwin.Patch}",
                $"{minDarwin.Major}.{minDarwin.Minor}.{minDarwin.Patch}") < 0)
            return (null, null); // No valid range

        return ($"{maxDarwin.Major}.{maxDarwin.Minor}.{maxDarwin.Patch}",
                $"{minDarwin.Major}.{minDarwin.Minor}.{minDarwin.Patch}");
    }
}
