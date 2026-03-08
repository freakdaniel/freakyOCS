using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Datasets;

namespace OcsNet.Core.Services;

public sealed record SmbiosData(
    string MLB,
    string ROM,
    string SystemProductName,
    string SystemSerialNumber,
    string SystemUUID
);

public sealed class SmbiosService
{
    private readonly ProcessRunner _process;
    private readonly AppUtils _utils;
    private readonly ILogger<SmbiosService>? _logger;

    public SmbiosService(ProcessRunner process, AppUtils utils, ILogger<SmbiosService>? logger = null)
    {
        _process = process;
        _utils = utils;
        _logger = logger;
    }

    public string GenerateRandomMac()
    {
        var bytes = new byte[6];
        Random.Shared.NextBytes(bytes);
        return Convert.ToHexString(bytes);
    }

    public async Task<SmbiosData> GenerateSmbiosAsync(string smbiosModel, CancellationToken ct = default)
    {
        var macserialPath = FindMacserial();
        var randomMac = GenerateRandomMac();

        string? serial = null;
        string? mlb = null;

        if (macserialPath is not null)
        {
            var result = await _process.RunAsync(macserialPath, ["-g", "--model", smbiosModel], cancellationToken: ct);
            if (result.Success && result.Output.Contains(" | "))
            {
                var parts = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Split(" | ");
                if (parts.Length >= 2)
                {
                    serial = parts[0].Trim();
                    mlb = parts[1].Trim();
                }
            }
        }

        return new SmbiosData(
            MLB: mlb ?? GenerateFallbackMlb(),
            ROM: randomMac,
            SystemProductName: smbiosModel,
            SystemSerialNumber: serial ?? GenerateFallbackSerial(),
            SystemUUID: Guid.NewGuid().ToString().ToUpperInvariant()
        );
    }

    public string? FindMacserial()
    {
        string[] candidates;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            candidates = ["macserial.exe"];
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            candidates = ["macserial.linux", "macserial"];
        else
            candidates = ["macserial"];

        foreach (var dir in GetBinarySearchPaths())
        foreach (var binary in candidates)
        {
            var path = Path.Combine(dir, binary);
            if (File.Exists(path))
                return path;
        }

        _logger?.LogWarning("macserial binary not found");
        return null;
    }

    /// <summary>
    /// Returns candidate directories to search for bundled binaries (macserial, iasl…).
    /// Searches the exe directory, cwd, and up to 4 parent levels of each — both the
    /// root dir itself and its Scripts/ subdirectory.
    /// </summary>
    internal static IEnumerable<string> GetBinarySearchPaths()
    {
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        void Enqueue(string? d)
        {
            if (!string.IsNullOrEmpty(d) && seen.Add(d))
                result.Add(d);
        }

        foreach (var root in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = root;
            for (int level = 0; level <= 4; level++)
            {
                Enqueue(dir);
                Enqueue(Path.Combine(dir, "Scripts"));
                var parent = Path.GetDirectoryName(dir);
                if (parent is null || parent == dir) break;
                dir = parent;
            }
        }

        return result;
    }

    private static string GenerateFallbackSerial()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var buf = new char[12];
        for (int i = 0; i < buf.Length; i++)
            buf[i] = chars[Random.Shared.Next(chars.Length)];
        return new string(buf);
    }

    private static string GenerateFallbackMlb()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var buf = new char[17];
        for (int i = 0; i < buf.Length; i++)
            buf[i] = chars[Random.Shared.Next(chars.Length)];
        return new string(buf);
    }

    public string SelectSmbiosModel(HardwareReport report, string macosVersion)
    {
        var platform = report.Motherboard?.Name?.Contains("NUC") == true
            ? "NUC"
            : report.Motherboard?.Platform ?? "Desktop";
        var codename = report.Cpu?.Codename ?? "";
        var coreCount = report.Cpu?.CoreCount ?? 4;
        var darwinVersion = OsData.ParseDarwinVersion(macosVersion);

        // Default fallback
        var smbiosModel = platform == "Laptop"
            ? "MacBookPro16,2"
            : darwinVersion.Major < 25 ? "iMacPro1,1" : "MacPro7,1";

        // Legacy CPU handling
        if (codename is "Lynnfield" or "Clarkdale" &&
            !report.Cpu!.ProcessorName.Contains("Xeon") &&
            darwinVersion.Major < 19)
        {
            smbiosModel = codename == "Lynnfield" ? "iMac11,1" : "iMac11,2";
        }
        else if (IsLegacyXeonPlatform(codename))
        {
            smbiosModel = darwinVersion.Major < 19 ? "MacPro5,1" : "MacPro6,1";
        }
        else if ((codename.Contains("Sandy Bridge") || codename.Contains("Ivy Bridge")) && darwinVersion.Major < 22)
        {
            smbiosModel = "MacPro6,1";
        }

        // Desktop with discrete GPU - return early
        if (platform != "Laptop" && HasDiscreteGpu(report))
            return smbiosModel;

        // Platform-specific selection based on codename
        smbiosModel = SelectByCodename(codename, platform, coreCount, darwinVersion, report) ?? smbiosModel;

        return smbiosModel;
    }

    private static bool IsLegacyXeonPlatform(string codename) =>
        codename is "Beckton" or "Westmere-EX" or "Gulftown" or "Westmere-EP" or "Clarkdale"
            or "Lynnfield" or "Jasper Forest" or "Gainestown" or "Bloomfield";

    private static bool HasDiscreteGpu(HardwareReport report) =>
        report.Gpus?.LastOrDefault().Value?.DeviceType != "Integrated GPU";

    private string? SelectByCodename(string codename, string platform, int coreCount,
        (int Major, int Minor, int Patch) darwin, HardwareReport report)
    {
        var hasIgpu = report.Gpus?.FirstOrDefault().Value?.DeviceType == "Integrated GPU";

        return codename switch
        {
            "Arrandale" or "Clarksfield" => "MacBookPro6,1",

            var c when c.Contains("Sandy Bridge") => platform switch
            {
                "Desktop" => "iMac12,2",
                "NUC" => coreCount < 4 ? "Macmini5,1" : "Macmini5,3",
                _ => coreCount < 4 ? "MacBookPro8,1" : "MacBookPro8,2"
            },

            var c when c.Contains("Ivy Bridge") => platform switch
            {
                "Desktop" => hasIgpu ? "iMac13,1" : "iMac13,2",
                "NUC" => coreCount < 4 ? "Macmini6,1" : "Macmini6,2",
                _ => coreCount < 4 ? "MacBookPro10,2" : "MacBookPro10,1"
            },

            var c when c.Contains("Haswell") => platform switch
            {
                "Desktop" => hasIgpu ? "iMac14,4" : "iMac15,1",
                "NUC" => "Macmini7,1",
                _ => coreCount < 4 ? "MacBookPro11,1" : "MacBookPro11,5"
            },

            var c when c.Contains("Broadwell") => platform switch
            {
                "Desktop" => hasIgpu ? "iMac16,2" : "iMac17,1",
                "NUC" => "iMac16,1",
                _ => coreCount < 4 ? "MacBookPro12,1" : "MacBookPro11,5"
            },

            var c when c.Contains("Skylake") => platform == "Laptop"
                ? (coreCount < 4 ? "MacBookPro13,1" : "MacBookPro13,3")
                : "iMac17,1",

            var c when c.Contains("Amber Lake") || c.Contains("Kaby Lake") => platform == "Laptop"
                ? (coreCount < 4 ? "MacBookPro14,1" : "MacBookPro14,3")
                : (hasIgpu ? "iMac18,1" : "iMac18,3"),

            var c when c.Contains("Cannon Lake") || c.Contains("Whiskey Lake") ||
                       c.Contains("Coffee Lake") || c.Contains("Comet Lake") =>
                SelectCoffeeLakeSmbios(c, platform, coreCount, darwin, report),

            var c when c.Contains("Ice Lake") => "MacBookAir9,1",

            _ => null
        };
    }

    private string SelectCoffeeLakeSmbios(string codename, string platform, int coreCount,
        (int Major, int Minor, int Patch) darwin, HardwareReport report)
    {
        if (platform == "Desktop")
        {
            if (codename.Contains("Comet Lake"))
                return coreCount < 10 ? "iMac20,1" : "iMac20,2";

            return darwin.Major < 18 ? "iMac18,3" : "iMac19,1";
        }

        if (platform == "Laptop")
        {
            var cpuName = report.Cpu?.ProcessorName ?? "";
            if (cpuName.Contains("-8"))
                return coreCount < 6 ? "MacBookPro15,2" : "MacBookPro15,3";

            return coreCount < 6 ? "MacBookPro16,3" : "MacBookPro16,1";
        }

        return "Macmini8,1";
    }
}

// Hardware report models for SMBIOS selection
public sealed class HardwareReport
{
    public CpuInfo? Cpu { get; set; }
    public MotherboardInfo? Motherboard { get; set; }
    public Dictionary<string, GpuInfo>? Gpus { get; set; }
    public Dictionary<string, MonitorInfo>? Monitors { get; set; }
    public Dictionary<string, AudioInfo>? Sound { get; set; }
    public Dictionary<string, NetworkInfo>? Network { get; set; }
    public Dictionary<string, StorageInfo>? StorageControllers { get; set; }
    public Dictionary<string, BluetoothInfo>? Bluetooth { get; set; }
    public Dictionary<string, BiometricInfo>? Biometric { get; set; }
}

public sealed class CpuInfo
{
    public string ProcessorName { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Codename { get; set; } = "";
    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }
    public HashSet<string> SimdFeatures { get; set; } = [];
}

public sealed class MotherboardInfo
{
    public string Name { get; set; } = "";
    public string Platform { get; set; } = "Desktop";
}

public sealed class GpuInfo
{
    public string Manufacturer { get; set; } = "";
    public string Codename { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public (string? Max, string? Min)? Compatibility { get; set; }
    public (string Max, string Min)? OclpCompatibility { get; set; }
}

public sealed class MonitorInfo
{
    public string ConnectorType { get; set; } = "";
    public string? ConnectedGpu { get; set; }
}

public sealed class AudioInfo
{
    public string DeviceId { get; set; } = "";
    public string BusType { get; set; } = "";
    public List<string>? AudioEndpoints { get; set; }
    public (string? Max, string? Min)? Compatibility { get; set; }
}

public sealed class NetworkInfo
{
    public string DeviceId { get; set; } = "";
    public string BusType { get; set; } = "";
    public (string? Max, string? Min)? Compatibility { get; set; }
    public (string Max, string Min)? OclpCompatibility { get; set; }
}

public sealed class StorageInfo
{
    public string DeviceId { get; set; } = "";
    public string SubsystemId { get; set; } = "";
    public string BusType { get; set; } = "";
    public (string? Max, string? Min)? Compatibility { get; set; }
}

public sealed class BluetoothInfo
{
    public string DeviceId { get; set; } = "";
    public (string? Max, string? Min)? Compatibility { get; set; }
}

public sealed class BiometricInfo
{
    public string DeviceType { get; set; } = "";
    public (string? Max, string? Min)? Compatibility { get; set; }
}
