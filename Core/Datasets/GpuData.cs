namespace OcsNet.Core.Datasets;

public static class GpuData
{
    // GCN codenames — resource: https://en.wikipedia.org/wiki/Graphics_Core_Next
    public static readonly string[] AmdCodenames =
    [
        "Hainan",
        "Oland",
        "Cape Verde",
        "Pitcairn",
        "Tahiti",
        "Bonaire",
        "Hawaii",
        "Tonga",
        "Fiji",
    ];

    // RDNA codenames
    public static readonly string[] RdnaCodenames =
    [
        "Navi 10", "Navi 12", "Navi 14",  // RDNA 1
        "Navi 21", "Navi 22", "Navi 23", "Navi 24",  // RDNA 2
        "Navi 31", "Navi 32", "Navi 33",  // RDNA 3
    ];

    // Device ID prefixes for GPU families
    private static readonly Dictionary<string, string> AmdDeviceIdPrefixes = new()
    {
        // Polaris
        ["67DF"] = "Polaris 10",
        ["67FF"] = "Polaris 11",
        ["699F"] = "Polaris 12",
        ["694C"] = "Polaris 22",
        // Vega
        ["687F"] = "Vega 10",
        ["66AF"] = "Vega 20",
        // RDNA
        ["7310"] = "Navi 10",
        ["7312"] = "Navi 12",
        ["7318"] = "Navi 10",
        ["7340"] = "Navi 14",
        // RDNA 2
        ["73BF"] = "Navi 21",
        ["73DF"] = "Navi 22",
        ["73EF"] = "Navi 23",
        ["73FF"] = "Navi 24",
        ["743F"] = "Navi 24",
        // RDNA 3
        ["744C"] = "Navi 31",
        ["7480"] = "Navi 33",
    };

    private static readonly Dictionary<string, string> IntelDeviceIdPrefixes = new()
    {
        // Skylake
        ["191"] = "Skylake GT2",
        ["192"] = "Skylake GT3",
        ["193"] = "Skylake GT4",
        // Kaby Lake
        ["591"] = "Kaby Lake GT2",
        ["592"] = "Kaby Lake GT3",
        ["593"] = "Kaby Lake GT4",
        ["87C0"] = "Kaby Lake GT2",
        // Coffee Lake
        ["3E9"] = "Coffee Lake GT2",
        // Ice Lake
        ["8A5"] = "Ice Lake GT2",
        // Tiger Lake
        ["9A4"] = "Tiger Lake GT2",
        // Alder Lake
        ["468"] = "Alder Lake GT1",
        ["46A"] = "Alder Lake GT2",
        // Raptor Lake (same as ADL)
        // Arc
        ["56A"] = "Alchemist",
    };

    /// <summary>
    /// Determines GPU codename based on device ID and manufacturer.
    /// </summary>
    public static string GetCodename(string deviceId, string manufacturer)
    {
        if (string.IsNullOrEmpty(deviceId))
            return "Unknown";

        // Extract just the device part (after vendor ID)
        var parts = deviceId.Split('-');
        var devId = parts.Length > 1 ? parts[1] : deviceId;
        devId = devId.ToUpperInvariant();

        if (manufacturer.Contains("AMD") || manufacturer == "1002")
        {
            foreach (var (prefix, codename) in AmdDeviceIdPrefixes)
            {
                if (devId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return codename;
            }

            // Fallback based on device ID ranges
            if (devId.StartsWith("67")) return "Polaris";
            if (devId.StartsWith("69")) return "Polaris";
            if (devId.StartsWith("68")) return "Vega";
            if (devId.StartsWith("66")) return "Vega";
            if (devId.StartsWith("73")) return "Navi";
            if (devId.StartsWith("74")) return "Navi 3x";
            if (devId.StartsWith("15")) return "Vega (APU)";
            if (devId.StartsWith("16")) return "Vega (APU)";

            return "Unknown AMD";
        }

        if (manufacturer.Contains("Intel") || manufacturer == "8086")
        {
            foreach (var (prefix, codename) in IntelDeviceIdPrefixes)
            {
                if (devId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return codename;
            }

            // Fallback based on device ID ranges
            if (devId.StartsWith("01")) return "Sandy Bridge";
            if (devId.StartsWith("04")) return "Haswell";
            if (devId.StartsWith("16")) return "Broadwell";
            if (devId.StartsWith("19")) return "Skylake";
            if (devId.StartsWith("59")) return "Kaby Lake";
            if (devId.StartsWith("3E")) return "Coffee Lake";
            if (devId.StartsWith("9B")) return "Comet Lake";
            if (devId.StartsWith("8A")) return "Ice Lake";
            if (devId.StartsWith("9A")) return "Tiger Lake";
            if (devId.StartsWith("46")) return "Alder Lake";
            if (devId.StartsWith("A7")) return "Raptor Lake";
            if (devId.StartsWith("56")) return "Arc Alchemist";

            return "Unknown Intel";
        }

        if (manufacturer.Contains("NVIDIA") || manufacturer == "10DE")
        {
            // NVIDIA codenames based on device ID ranges
            if (devId.StartsWith("10")) return "Pascal";
            if (devId.StartsWith("1B")) return "Pascal";
            if (devId.StartsWith("1C")) return "Pascal";
            if (devId.StartsWith("1D")) return "Volta";
            if (devId.StartsWith("1E")) return "Turing";
            if (devId.StartsWith("1F")) return "Turing";
            if (devId.StartsWith("20")) return "Turing";
            if (devId.StartsWith("21")) return "Turing";
            if (devId.StartsWith("22")) return "Ampere";
            if (devId.StartsWith("24")) return "Ampere";
            if (devId.StartsWith("25")) return "Ampere";
            if (devId.StartsWith("26")) return "Ada Lovelace";
            if (devId.StartsWith("27")) return "Ada Lovelace";
            if (devId.StartsWith("28")) return "Blackwell";

            return "Unknown NVIDIA";
        }

        return "Unknown";
    }
}
