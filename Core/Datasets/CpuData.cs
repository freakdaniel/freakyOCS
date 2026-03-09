using System.Text.RegularExpressions;

namespace OcsNet.Core.Datasets;

public static class CpuData
{
    public static readonly string[] AmdCpuGenerations =
    [
        "Summit Ridge",
        "Whitehaven",
        "Raven Ridge",
        "Great Horned Owl",
        "Dalí",
        "Banded Kestrel",
        "River Hawk",
        "Pinnacle Ridge",
        "Colfax",
        "Picasso",
        "Grey Hawk",
        "Matisse",
        "Castle Peak",
        "Renoir",
        "Lucienne",
        "Vermeer",
        "Cezanne",
        "Barceló",
        "Rembrandt",
        "Mendocino",
        "Barceló-R",
        "Rembrandt-R",
        "V3000",
        "Chagall",
        "Raphael",
        "Storm Peak",
        "Phoenix",
        "Dragon Range",
        "Hawk Point",
        "Granite Ridge",
        "Strix Point",
    ];

    public static readonly string[] IntelCpuGenerations =
    [
        "Arrow Lake-S",
        "Arrow Lake-H",
        "Arrow Lake-HX",
        "Arrow Lake-U",
        "Lunar Lake",
        "Meteor Lake-H",
        "Meteor Lake-U",
        "Raptor Lake-S",
        "Raptor Lake-E",
        "Raptor Lake-HX",
        "Raptor Lake-H",
        "Raptor Lake-PX",
        "Raptor Lake-P",
        "Raptor Lake-U",
        "Alder Lake-S",
        "Alder Lake-HX",
        "Alder Lake-H",
        "Alder Lake-P",
        "Alder Lake-U",
        "Alder Lake-N",
        "Lakefield",
        "Rocket Lake-S",
        "Rocket Lake-E",
        "Tiger Lake-H",
        "Tiger Lake-B",
        "Tiger Lake-UP3",
        "Tiger Lake-H35",
        "Tiger Lake-UP4",
        "Ice Lake-U",
        "Ice Lake-SP",
        "Comet Lake-S",
        "Comet Lake-W",
        "Comet Lake-H",
        "Comet Lake-U",
        "Coffee Lake-S",
        "Coffee Lake-E",
        "Coffee Lake-H",
        "Coffee Lake-U",
        "Cannon Lake-U",
        "Whiskey Lake-U",
        "Kaby Lake",
        "Kaby Lake-H",
        "Kaby Lake-X",
        "Kaby Lake-G",
        "Amber Lake-Y",
        "Cascade Lake-X",
        "Cascade Lake-P",
        "Cascade Lake-W",
        "Skylake",
        "Skylake-X",
        "Broadwell",
        "Broadwell-H",
        "Broadwell-U",
        "Broadwell-Y",
        "Broadwell-E",
        "Haswell",
        "Haswell-ULT",
        "Haswell-ULX",
        "Haswell-H",
        "Haswell-E",
        "Haswell-EP",
        "Haswell-EX",
        "Ivy Bridge",
        "Ivy Bridge-E",
        "Sandy Bridge",
        "Sandy Bridge-E",
        "Beckton",
        "Westmere-EX",
        "Gulftown",
        "Westmere-EP",
        "Clarkdale",
        "Arrandale",
        "Lynnfield",
        "Jasper Forest",
        "Clarksfield",
        "Gainestown",
        "Bloomfield",
    ];

    /// <summary>
    /// Determines CPU codename based on processor name and manufacturer.
    /// </summary>
    public static string GetCodename(string processorName, string manufacturer)
    {
        processorName = processorName.ToUpperInvariant();
        
        if (manufacturer == "AuthenticAMD" || manufacturer.Contains("AMD"))
        {
            // AMD Ryzen detection based on model numbers
            if (processorName.Contains("RYZEN 9 9") || processorName.Contains("RYZEN 7 9"))
                return "Granite Ridge";
            if (processorName.Contains("RYZEN 9 7") || processorName.Contains("RYZEN 7 7") || processorName.Contains("RYZEN 5 7"))
                return processorName.Contains("X3D") ? "Raphael" : "Raphael";
            if (processorName.Contains("RYZEN 9 6") || processorName.Contains("RYZEN 7 6") || processorName.Contains("RYZEN 5 6"))
                return "Rembrandt";
            if (processorName.Contains("RYZEN 9 5") || processorName.Contains("RYZEN 7 5") || processorName.Contains("RYZEN 5 5"))
                return processorName.Contains("G") ? "Cezanne" : "Vermeer";
            if (processorName.Contains("RYZEN 9 3") || processorName.Contains("RYZEN 7 3") || processorName.Contains("RYZEN 5 3"))
                return processorName.Contains("G") ? "Picasso" : "Matisse";
            if (processorName.Contains("RYZEN 7 2") || processorName.Contains("RYZEN 5 2") || processorName.Contains("RYZEN 3 2"))
                return processorName.Contains("G") ? "Raven Ridge" : "Pinnacle Ridge";
            if (processorName.Contains("RYZEN 7 1") || processorName.Contains("RYZEN 5 1") || processorName.Contains("RYZEN 3 1"))
                return "Summit Ridge";
            if (processorName.Contains("THREADRIPPER"))
                return "Castle Peak";
            
            return "Unknown AMD";
        }

        // Intel detection
        if (processorName.Contains("ULTRA"))
        {
            if (processorName.Contains("288") || processorName.Contains("285"))
                return "Arrow Lake-S";
            if (processorName.Contains("256") || processorName.Contains("258"))
                return "Lunar Lake";
            return "Meteor Lake";
        }
        
        // ── Mobile H / HX / P series ─────────────────────────────────────────
        // Must precede desktop model-number checks because some desktop model
        // numbers (e.g. 13900) also appear in mobile names (i9-13900H).
        // Source: https://dortania.github.io/OpenCore-Install-Guide/macos-limits.html
        //   "Intel's Core 'i' and Xeon series laptop CPUs – Arrandale through Ice Lake
        //    are supported by this guide." (Tiger Lake+ via community patches / OCLP)

        // 13th gen HX (Raptor Lake-HX): i9-13980HX, i9-13900HX, i7-13700HX, i5-13600HX
        if (Regex.IsMatch(processorName, @"\b13\d{3}HX\b"))
            return "Raptor Lake-HX";
        // 13th gen H (Raptor Lake-H): i9-13900H, i7-13700H, i7-13620H, i5-13500H, i5-13450H, i5-13420H
        if (Regex.IsMatch(processorName, @"\b13\d{3}H[K]?\b"))
            return "Raptor Lake-H";
        // 13th gen P (Raptor Lake-P): i7-1370P, i7-1360P, i5-1340P
        if (Regex.IsMatch(processorName, @"\b13\d{2}P\b"))
            return "Raptor Lake-P";

        // 12th gen HX (Alder Lake-HX): i9-12900HX, i7-12800HX, i7-12650HX, i5-12600HX
        if (Regex.IsMatch(processorName, @"\b12\d{3}HX\b"))
            return "Alder Lake-HX";
        // 12th gen H (Alder Lake-H): i9-12900H, i9-12900HK, i7-12700H, i7-12650H, i5-12500H, i5-12450H
        if (Regex.IsMatch(processorName, @"\b12\d{3}H[K]?\b"))
            return "Alder Lake-H";
        // 12th gen P (Alder Lake-P): i7-1280P, i7-1270P, i7-1260P, i5-1250P, i5-1240P
        if (Regex.IsMatch(processorName, @"\b12\d{2}P\b"))
            return "Alder Lake-P";

        // 11th gen H (Tiger Lake-H): i9-11980HK, i9-11900H, i7-11850H, i7-11800H, i5-11400H
        if (Regex.IsMatch(processorName, @"\b11\d{3}H[K]?\b"))
            return "Tiger Lake-H";

        // 10th gen H (Comet Lake-H): i9-10980HK, i9-10885H, i7-10875H, i7-10750H, i5-10500H, i5-10300H
        if (Regex.IsMatch(processorName, @"\b10\d{3}H[K]?\b"))
            return "Comet Lake-H";

        // 9th gen H (Coffee Lake-H): i9-9980HK, i9-9880H, i7-9850H, i7-9750H, i5-9400H, i5-9300H
        if (Regex.IsMatch(processorName, @"\b9\d{3}H[K]?\b"))
            return "Coffee Lake-H";
        // 8th gen H (Coffee Lake-H): i9-8950HK, i7-8850H, i7-8750H, i5-8400H, i5-8300H
        if (Regex.IsMatch(processorName, @"\b8\d{3}H[K]?\b"))
            return "Coffee Lake-H";

        // 7th gen H (Kaby Lake-H): i7-7820HK, i7-7820HQ, i7-7700HQ, i5-7300HQ
        if (Regex.IsMatch(processorName, @"\b7\d{3}H[QK]?\b"))
            return "Kaby Lake-H";

        // 6th gen H (Skylake-H): i7-6820HK, i7-6820HQ, i7-6700HQ, i5-6440HQ, i5-6300HQ
        if (Regex.IsMatch(processorName, @"\b6\d{3}H[QK]?\b"))
            return "Skylake-H";

        // 5th gen H (Broadwell-H): i7-5950HQ, i7-5850HQ, i7-5750HQ, i7-5700HQ
        if (Regex.IsMatch(processorName, @"\b5\d{3}HQ?\b"))
            return "Broadwell-H";

        // 4th gen mobile M-suffix (Haswell mobile): i7-4800MQ, i7-4900MQ, i7-4712MQ
        if (Regex.IsMatch(processorName, @"\b4\d{3}M[QE]?\b"))
            return "Haswell-H";

        // ── Desktop / HEDT series ─────────────────────────────────────────────
        if (processorName.Contains("14") && (processorName.Contains("14900") || processorName.Contains("14700") || processorName.Contains("14600")))
            return "Raptor Lake-S";
        if (processorName.Contains("13") && (processorName.Contains("13900") || processorName.Contains("13700") || processorName.Contains("13600")))
            return "Raptor Lake-S";
        if (processorName.Contains("12") && (processorName.Contains("12900") || processorName.Contains("12700") || processorName.Contains("12600")))
            return "Alder Lake-S";
        if (processorName.Contains("11") && (processorName.Contains("11900") || processorName.Contains("11700") || processorName.Contains("11600")))
            return "Rocket Lake-S";
        if (processorName.Contains("10") && (processorName.Contains("10900") || processorName.Contains("10700") || processorName.Contains("10600")))
            return "Comet Lake-S";
        if (processorName.Contains("9") && (processorName.Contains("9900") || processorName.Contains("9700") || processorName.Contains("9600")))
            return "Coffee Lake-S";
        if (processorName.Contains("8") && (processorName.Contains("8700") || processorName.Contains("8600") || processorName.Contains("8400")))
            return "Coffee Lake-S";
        if (processorName.Contains("7") && (processorName.Contains("7700") || processorName.Contains("7600") || processorName.Contains("7500")))
            return "Kaby Lake";
        if (processorName.Contains("6") && (processorName.Contains("6700") || processorName.Contains("6600") || processorName.Contains("6500")))
            return "Skylake";
        if (processorName.Contains("5") && (processorName.Contains("5775") || processorName.Contains("5675")))
            return "Broadwell";
        if (processorName.Contains("4") && (processorName.Contains("4790") || processorName.Contains("4770") || processorName.Contains("4690")))
            return "Haswell";
        if (processorName.Contains("3") && (processorName.Contains("3770") || processorName.Contains("3570")))
            return "Ivy Bridge";
        if (processorName.Contains("2") && (processorName.Contains("2700") || processorName.Contains("2600") || processorName.Contains("2500")))
            return "Sandy Bridge";

        // ── Mobile U / G series ────────────────────────────────────────────────
        if (processorName.Contains("1365U") || processorName.Contains("1355U") || processorName.Contains("1345U"))
            return "Raptor Lake-U";
        if (processorName.Contains("1265U") || processorName.Contains("1255U") || processorName.Contains("1245U"))
            return "Alder Lake-U";
        if (processorName.Contains("1165G") || processorName.Contains("1135G"))
            return "Tiger Lake-UP3";
        if (processorName.Contains("1065G") || processorName.Contains("1035G"))
            return "Ice Lake-U";

        return "Unknown Intel";
    }
}
