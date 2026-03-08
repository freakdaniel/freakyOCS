using Claunia.PropertyList;
using OcsNet.Core.Datasets;
using Microsoft.Extensions.Logging;
using static OcsNet.Core.Services.PlistHelper;

namespace OcsNet.Core.Services;

/// <summary>
/// Service for managing kext selection, extraction of PCI IDs from kexts,
/// and determining required kexts based on hardware.
/// </summary>
public sealed class KextService
{
    private readonly AppUtils _utils;
    private readonly ILogger<KextService>? _logger;

    private static readonly HashSet<string> MatchingKeys =
    [
        "IOPCIMatch",
        "IONameMatch",
        "IOPCIPrimaryMatch",
        "idProduct",
        "idVendor",
        "HDAConfigDefault"
    ];

    public KextService(AppUtils utils, ILogger<KextService>? logger = null)
    {
        _utils = utils;
        _logger = logger;
    }

    /// <summary>
    /// Extracts PCI device IDs from a kext's Info.plist IOKitPersonalities.
    /// </summary>
    public async Task<List<string>> ExtractPciIdsAsync(string kextPath, CancellationToken ct = default)
    {
        var pciIds = new List<string>();

        var plistPath = Path.Combine(kextPath, "Contents", "Info.plist");
        if (!File.Exists(plistPath))
            return pciIds;

        try
        {
            var plist = await _utils.ReadPlistAsync<NSDictionary>(plistPath, ct);
            if (plist is null || !plist.TryGetValue("IOKitPersonalities", out var personalities))
                return pciIds;

            var personalitiesDict = personalities as NSDictionary;
            if (personalitiesDict is null)
                return pciIds;

            foreach (var (_, value) in personalitiesDict)
            {
                if (value is not NSDictionary properties)
                    continue;

                var matchKey = MatchingKeys.FirstOrDefault(k => properties.ContainsKey(k));
                if (matchKey is null)
                    continue;

                var matchValue = properties[matchKey];

                switch (matchKey)
                {
                    case "IOPCIMatch" or "IOPCIPrimaryMatch":
                        // Format: "0x12345678" where last 4 digits = vendor, first 4 = device
                        if (matchValue is NSString matchStr)
                        {
                            foreach (var pciId in matchStr.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            {
                                var id = pciId.Replace("0x", "").ToUpperInvariant();
                                if (id.Length >= 8)
                                {
                                    var vendorId = id[4..8];
                                    var deviceId = id[..4];
                                    pciIds.Add($"{vendorId}-{deviceId}");
                                }
                            }
                        }
                        break;

                    case "IONameMatch":
                        // Format: "pciVVVV,DDDD"
                        if (matchValue is NSArray nameArray)
                        {
                            foreach (var item in nameArray)
                            {
                                if (item is NSString nameStr)
                                {
                                    var name = nameStr.Content.Replace("pci", "");
                                    var parts = name.Split(',');
                                    if (parts.Length == 2)
                                    {
                                        var vendorId = parts[0].ToUpperInvariant().PadLeft(4, '0');
                                        var deviceId = parts[1].ToUpperInvariant().PadLeft(4, '0');
                                        pciIds.Add($"{vendorId}-{deviceId}");
                                    }
                                }
                            }
                        }
                        break;

                    case "idProduct":
                        // USB device
                        if (properties.TryGetValue("idVendor", out var vendorObj) &&
                            vendorObj is NSNumber vendorNum &&
                            matchValue is NSNumber productNum)
                        {
                            var vendor = vendorNum.ToInt().ToString("X4");
                            var product = productNum.ToInt().ToString("X4");
                            pciIds.Add($"{vendor}-{product}");
                        }
                        break;

                    case "HDAConfigDefault":
                        // Audio codec
                        if (matchValue is NSArray hdaArray)
                        {
                            foreach (var item in hdaArray)
                            {
                                if (item is NSDictionary hdaDict &&
                                    hdaDict.TryGetValue("CodecID", out var codecIdObj) &&
                                    codecIdObj is NSNumber codecIdNum)
                                {
                                    var codecId = codecIdNum.ToLong().ToString("X8");
                                    pciIds.Add($"{codecId[..4]}-{codecId[4..]}");
                                }
                            }
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to extract PCI IDs from {Path}", kextPath);
        }

        return pciIds.Distinct().OrderBy(id => id).ToList();
    }

    /// <summary>
    /// Determines which kexts are required based on hardware report.
    /// </summary>
    public HashSet<string> SelectRequiredKexts(
        HardwareReport report,
        string macosVersion,
        bool needsOclp = false)
    {
        var kexts = new HashSet<string>
        {
            // Always required
            "Lilu",
            "VirtualSMC"
        };

        var darwin = OsData.ParseDarwinVersion(macosVersion);
        var cpuManufacturer = report.Cpu?.Manufacturer ?? "";
        var cpuCodename = report.Cpu?.Codename ?? "";
        var platform = report.Motherboard?.Platform ?? "Desktop";

        // CPU-based kexts
        if (cpuManufacturer.Contains("Intel"))
        {
            kexts.Add("SMCProcessor");
            kexts.Add("SMCSuperIO");
        }
        else if (cpuManufacturer.Contains("AMD"))
        {
            kexts.Add("AMDRyzenCPUPowerManagement");
            kexts.Add("SMCAMDProcessor");

            // AMD needs MCE reporter disabler on newer macOS
            if (darwin.Major >= 21)
                kexts.Add("AppleMCEReporterDisabler");
        }

        // Platform-based kexts
        if (platform == "Laptop")
        {
            kexts.Add("SMCBatteryManager");
            kexts.Add("SMCLightSensor");
            kexts.Add("VoodooPS2Controller");

            // Dell sensors
            if (report.Motherboard?.Name?.Contains("DELL") == true)
                kexts.Add("SMCDellSensors");
        }

        // RestrictEvents for older CPUs or Sonoma+
        if (!cpuCodename.Contains("Core") || darwin.Major >= 23)
            kexts.Add("RestrictEvents");

        // Audio codec support
        if (report.Sound is not null)
        {
            foreach (var (_, audio) in report.Sound)
            {
                if (CodecLayouts.Data.ContainsKey(audio.DeviceId))
                {
                    // Tahoe removed AppleHDA
                    if (darwin.Major >= 25)
                    {
                        // User needs OCLP for AppleALC on Tahoe
                        // Or use VoodooHDA
                    }
                    else
                    {
                        kexts.Add("AppleALC");
                    }
                    break;
                }
            }
        }

        // CryptexFixup for non-AVX2 CPUs on Ventura+
        if (darwin.Major >= 22 && report.Cpu?.SimdFeatures?.Contains("AVX2") != true)
            kexts.Add("CryptexFixup");

        // GPU kexts
        if (report.Gpus is not null)
        {
            foreach (var (gpuName, gpu) in report.Gpus)
            {
                if (gpu.DeviceType == "Integrated GPU")
                {
                    if (gpu.Manufacturer.Contains("AMD"))
                        kexts.Add("NootedRed");
                    else if (gpu.Manufacturer.Contains("Intel"))
                        kexts.Add("WhateverGreen");
                }
                else if (gpu.DeviceType == "Discrete GPU")
                {
                    // NootRX for specific Navi cards
                    if (gpu.Codename is "Navi 22" or "Navi 21" or "Navi 23")
                    {
                        // NootRX conflicts with Intel iGPU
                        var hasIntelIgpu = report.Gpus.Values.Any(g =>
                            g.DeviceType == "Integrated GPU" && g.Manufacturer.Contains("Intel"));

                        if (!hasIntelIgpu)
                            kexts.Add("NootRX");
                        else
                            kexts.Add("WhateverGreen");
                    }
                    else if (gpu.Manufacturer.Contains("AMD") || gpu.Manufacturer.Contains("NVIDIA"))
                    {
                        kexts.Add("WhateverGreen");
                    }
                }
            }
        }

        // ASUS laptop quirk
        if (platform == "Laptop" &&
            (report.Motherboard?.Name?.Contains("ASUS") == true || kexts.Contains("NootedRed")))
            kexts.Add("ForgedInvariant");

        // Intel HEDT needs TSC sync
        if (IsIntelHedt(report.Cpu?.ProcessorName ?? "", cpuCodename))
            kexts.Add("CpuTscSync");

        // OCLP-related kexts
        if (needsOclp)
        {
            kexts.Add("AMFIPass");
            kexts.Add("RestrictEvents");
        }

        // Network kexts
        if (report.Network is not null)
        {
            foreach (var (_, network) in report.Network)
            {
                var deviceId = network.DeviceId.ToUpperInvariant();

                // Broadcom WiFi
                if (PciData.BroadcomWiFiIds.Any(id => deviceId.Contains(id)))
                {
                    kexts.Add("AirportBrcmFixup");
                    if (darwin.Major >= 23)
                        kexts.Add("IOSkywalkFamily");
                }

                // Intel WiFi
                if (PciData.IntelWiFiIds.Any(id => deviceId.Contains(id)))
                {
                    // AirportItlwm for native experience, itlwm for stability
                    if (darwin.Major >= 23)
                    {
                        kexts.Add("itlwm"); // More stable on Sonoma+
                    }
                    else
                    {
                        kexts.Add("AirportItlwm");
                    }
                    kexts.Add("IntelBluetoothFirmware");
                    kexts.Add("IntelBTPatcher");
                }

                // Intel Ethernet
                if (PciData.IntelMausiIds.Any(id => deviceId.Contains(id)))
                    kexts.Add("IntelMausi");

                // Realtek Ethernet
                if (PciData.RealtekRtl8111Ids.Any(id => deviceId.Contains(id)))
                    kexts.Add("RealtekRTL8111");
            }
        }

        // NVMe fix
        if (report.StorageControllers?.Values.Any(s => s.BusType == "NVMe") == true)
            kexts.Add("NVMeFix");

        // USB
        kexts.Add("USBToolBox");
        kexts.Add("UTBDefault");

        // Add dependencies
        AddDependencies(kexts);

        return kexts;
    }

    private static void AddDependencies(HashSet<string> kexts)
    {
        var toAdd = new HashSet<string>();

        foreach (var kextName in kexts)
        {
            var kextDef = KextData.Kexts.FirstOrDefault(k => k.Name == kextName);
            if (kextDef?.RequiresKexts is not null)
            {
                foreach (var dep in kextDef.RequiresKexts)
                    toAdd.Add(dep);
            }
        }

        foreach (var dep in toAdd)
            kexts.Add(dep);
    }

    private static bool IsIntelHedt(string processorName, string codename)
    {
        if (codename.EndsWith("-X") || codename.EndsWith("-W") ||
            codename.EndsWith("-E") || codename.EndsWith("-EP") || codename.EndsWith("-EX"))
            return true;

        return processorName.Contains("Xeon");
    }

    /// <summary>
    /// Validates kext compatibility with target macOS version.
    /// </summary>
    public bool IsKextCompatible(string kextName, string macosVersion)
    {
        var kextDef = KextData.Kexts.FirstOrDefault(k => k.Name == kextName);
        if (kextDef is null)
            return true; // Unknown kexts assumed compatible

        var darwin = OsData.ParseDarwinVersion(macosVersion);
        var minDarwin = OsData.ParseDarwinVersion(kextDef.EffectiveMin);
        var maxDarwin = OsData.ParseDarwinVersion(kextDef.EffectiveMax);

        return darwin.Major >= minDarwin.Major && darwin.Major <= maxDarwin.Major;
    }

    /// <summary>
    /// Generates the Kernel/Add array for config.plist from kext paths.
    /// </summary>
    public NSArray GenerateKernelAddArray(Dictionary<string, string> kextPaths)
    {
        var addArray = new NSArray();

        // Sort kexts by load order (Lilu first, then plugins, then others)
        var sortedKexts = kextPaths
            .OrderBy(kv => kv.Key == "Lilu" ? 0 :
                          kv.Key == "VirtualSMC" ? 1 :
                          kv.Key.StartsWith("SMC") ? 2 :
                          kv.Key == "WhateverGreen" ? 3 :
                          kv.Key == "AppleALC" ? 4 : 5)
            .ThenBy(kv => kv.Key);

        foreach (var (kextName, kextPath) in sortedKexts)
        {
            var entry = Dict(
                ("Arch", Str("x86_64")),
                ("BundlePath", Str($"{kextName}.kext")),
                ("Comment", Str("")),
                ("Enabled", Bool(true)),
                ("ExecutablePath", Str($"Contents/MacOS/{kextName}")),
                ("MaxKernel", Str("")),
                ("MinKernel", Str("")),
                ("PlistPath", Str("Contents/Info.plist"))
            );

            // Check for plugins
            var pluginsPath = Path.Combine(kextPath, "Contents", "PlugIns");
            if (Directory.Exists(pluginsPath))
            {
                foreach (var pluginDir in Directory.GetDirectories(pluginsPath, "*.kext"))
                {
                    var pluginName = Path.GetFileNameWithoutExtension(pluginDir);
                    var pluginEntry = Dict(
                        ("Arch", Str("x86_64")),
                        ("BundlePath", Str($"{kextName}.kext/Contents/PlugIns/{Path.GetFileName(pluginDir)}")),
                        ("Comment", Str("")),
                        ("Enabled", Bool(true)),
                        ("ExecutablePath", Str($"Contents/MacOS/{pluginName}")),
                        ("MaxKernel", Str("")),
                        ("MinKernel", Str("")),
                        ("PlistPath", Str("Contents/Info.plist"))
                    );
                    // Add plugin after parent
                    addArray.Add(pluginEntry);
                }
            }

            addArray.Add(entry);
        }

        return addArray;
    }

    /// <summary>
    /// Checks if a kext conflicts with another based on conflict groups.
    /// </summary>
    public bool HasConflict(string kextName, HashSet<string> enabledKexts)
    {
        var kextDef = KextData.Kexts.FirstOrDefault(k => k.Name == kextName);
        if (kextDef is null)
            return false;

        if (kextDef.ConflictGroupId is null)
            return false;

        foreach (var other in enabledKexts)
        {
            if (other == kextName)
                continue;

            var otherDef = KextData.Kexts.FirstOrDefault(k => k.Name == other);
            if (otherDef?.ConflictGroupId == kextDef.ConflictGroupId)
                return true;
        }

        return false;
    }
}
