namespace OcsNet.Core.Datasets;

public sealed record MacOsVersionInfo(
    string Name,
    int DarwinVersion,
    string MacosVersion,
    string ReleaseStatus = "final"
);

public static class OsData
{
    public static readonly MacOsVersionInfo[] MacosVersions =
    [
        new("High Sierra", 17, "10.13"),
        new("Mojave",      18, "10.14"),
        new("Catalina",    19, "10.15"),
        new("Big Sur",     20, "11"),
        new("Monterey",    21, "12"),
        new("Ventura",     22, "13"),
        new("Sonoma",      23, "14"),
        new("Sequoia",     24, "15"),
        new("Tahoe",       25, "26"),
    ];

    public static string GetLatestDarwinVersion(bool includeBeta = true)
    {
        foreach (var v in MacosVersions.Reverse())
        {
            if (includeBeta || v.ReleaseStatus == "final")
                return $"{v.DarwinVersion}.99.99";
        }
        return $"{MacosVersions[^1].DarwinVersion}.99.99";
    }

    public static string GetLowestDarwinVersion() =>
        $"{MacosVersions[0].DarwinVersion}.0.0";

    public static string? GetMacosNameByDarwin(string darwinVersion)
    {
        if (darwinVersion.Length < 2) return null;
        if (!int.TryParse(darwinVersion[..2], out var major)) return null;
        var info = MacosVersions.FirstOrDefault(v => v.DarwinVersion == major);
        if (info is null) return null;
        var suffix = info.ReleaseStatus == "final" ? "" : " (Beta)";
        return $"macOS {info.Name} {info.MacosVersion}{suffix}";
    }

    public static int CompareDarwinVersions(string a, string b)
    {
        var pa = ParseDarwinVersion(a);
        var pb = ParseDarwinVersion(b);
        int c = pa.Major.CompareTo(pb.Major);
        if (c != 0) return c;
        c = pa.Minor.CompareTo(pb.Minor);
        if (c != 0) return c;
        return pa.Patch.CompareTo(pb.Patch);
    }

    public static (int Major, int Minor, int Patch) ParseDarwinVersion(string version)
    {
        var parts = version.Split('.');
        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }
}
