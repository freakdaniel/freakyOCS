using System.Runtime.InteropServices;
using OcsNet.Core.Services;

namespace OcsNet.Core.Bridge;

/// <summary>
/// Maps internal HardwareReport to the shape expected by the React frontend.
/// Mirrors TypeScript types in Frontend/src/types/index.ts.
/// </summary>
public sealed class HardwareFrontendReport
{
    public FeCpuInfo Cpu { get; init; } = new("Unknown", "", "Unknown", 0, 0, 0, 1, 1, []);
    public FeGpuInfo[] Gpu { get; init; } = [];
    public FeAudioInfo[] Audio { get; init; } = [];
    public FeNetworkInfo[] Network { get; init; } = [];
    public FeStorageInfo[] Storage { get; init; } = [];
    public FeUsbControllerInfo[] Usb { get; init; } = [];
    public FeMotherboardInfo Motherboard { get; init; } = new("Unknown", "Unknown", null, null);
    public FeMemoryInfo Memory { get; init; } = new(0, []);
    public string GeneratedAt { get; init; } = DateTime.UtcNow.ToString("o");
    public string Platform { get; init; } = GetPlatform();

    public static HardwareFrontendReport From(HardwareReport report) =>
        new()
        {
            Cpu = MapCpu(report.Cpu),
            Gpu = MapGpus(report.Gpus),
            Audio = MapAudio(report.Sound),
            Network = MapNetwork(report.Network),
            Storage = MapStorage(report.StorageControllers),
            Usb = [],
            Motherboard = MapMotherboard(report.Motherboard),
            Memory = new FeMemoryInfo(0, []),
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            Platform = GetPlatform()
        };

    private static FeCpuInfo MapCpu(CpuInfo? cpu)
    {
        if (cpu is null)
            return new("Unknown", "", "Unknown", 0, 0, 0, 1, 1, []);

        return new(
            Name: cpu.ProcessorName,
            Codename: cpu.Codename,
            Vendor: cpu.Manufacturer switch
            {
                "GenuineIntel" => "Intel",
                "AuthenticAMD" => "AMD",
                _ => "Unknown"
            },
            Family: 0,
            Model: 0,
            Stepping: 0,
            Cores: cpu.CoreCount,
            Threads: cpu.ThreadCount > 0 ? cpu.ThreadCount : cpu.CoreCount,
            SupportedFeatures: [.. cpu.SimdFeatures]
        );
    }

    private static FeGpuInfo[] MapGpus(Dictionary<string, GpuInfo>? gpus)
    {
        if (gpus is null) return [];
        return [.. gpus.Select(kvp =>
        {
            var (vendorId, deviceId) = SplitId(kvp.Value.DeviceId);
            return new FeGpuInfo(
                Name: kvp.Key,
                Vendor: kvp.Value.Manufacturer,
                DeviceId: deviceId,
                Codename: NullIfEmpty(kvp.Value.Codename),
                Vram: null,
                Discrete: kvp.Value.DeviceType == "Discrete GPU"
            );
        })];
    }

    private static FeAudioInfo[] MapAudio(Dictionary<string, AudioInfo>? audio)
    {
        if (audio is null) return [];
        return [.. audio.Select(kvp =>
        {
            var (vendorId, deviceId) = SplitId(kvp.Value.DeviceId);
            return new FeAudioInfo(kvp.Key, vendorId, deviceId, null, null);
        })];
    }

    private static FeNetworkInfo[] MapNetwork(Dictionary<string, NetworkInfo>? network)
    {
        if (network is null) return [];
        return [.. network.Select(kvp =>
        {
            var (vendorId, deviceId) = SplitId(kvp.Value.DeviceId);
            var type = kvp.Value.BusType is "WiFi" or "Ethernet" or "USB"
                ? kvp.Value.BusType
                : "Ethernet";
            return new FeNetworkInfo(kvp.Key, type, vendorId, deviceId, null);
        })];
    }

    private static FeStorageInfo[] MapStorage(Dictionary<string, StorageInfo>? storage)
    {
        if (storage is null) return [];
        return [.. storage.Select(kvp =>
        {
            var (vendorId, deviceId) = SplitId(kvp.Value.DeviceId);
            var type = kvp.Value.BusType switch
            {
                "NVMe" => "NVMe",
                "SATA" => "SATA",
                "USB" => "USB",
                _ => "Unknown"
            };
            return new FeStorageInfo(kvp.Key, type, 0, NullIfEmpty(vendorId), NullIfEmpty(deviceId));
        })];
    }

    private static FeMotherboardInfo MapMotherboard(MotherboardInfo? mb)
    {
        if (mb is null)
            return new("Unknown", "Unknown", null, null);
        var parts = mb.Name.Split(' ', 2, StringSplitOptions.TrimEntries);
        return new(
            Manufacturer: parts.Length > 0 ? parts[0] : "Unknown",
            Model: parts.Length > 1 ? parts[1] : mb.Name,
            BiosVersion: null,
            Chipset: null
        );
    }

    private static (string VendorId, string DeviceId) SplitId(string id)
    {
        var idx = id.IndexOf('-');
        return idx >= 0 ? (id[..idx], id[(idx + 1)..]) : ("0000", id);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

    private static string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macOS";
        return "Linux";
    }
}

public sealed record FeCpuInfo(
    string Name, string Codename, string Vendor,
    int Family, int Model, int Stepping,
    int Cores, int Threads, string[] SupportedFeatures);

public sealed record FeGpuInfo(
    string Name, string Vendor, string DeviceId,
    string? Codename, int? Vram, bool Discrete);

public sealed record FeAudioInfo(
    string Name, string VendorId, string DeviceId,
    int? CodecId, int[]? SuggestedLayouts);

public sealed record FeNetworkInfo(
    string Name, string Type, string VendorId,
    string DeviceId, string? MacAddress);

public sealed record FeStorageInfo(
    string Name, string Type, long Size,
    string? VendorId, string? DeviceId);

public sealed record FeUsbControllerInfo(
    string Name, string Type, string VendorId,
    string DeviceId, int PortCount);

public sealed record FeMotherboardInfo(
    string Manufacturer, string Model,
    string? BiosVersion, string? Chipset);

public sealed record FeMemorySlot(long Size, int Speed, string Type, string? Manufacturer);
public sealed record FeMemoryInfo(long TotalSize, FeMemorySlot[] Slots);
