using Claunia.PropertyList;
using OcsNet.Core.Datasets;
using static OcsNet.Core.Services.PlistHelper;

namespace OcsNet.Core.Services;

/// <summary>
/// Service for generating OpenCore config.plist based on hardware report and settings.
/// </summary>
public sealed class ConfigService
{
    private readonly AppUtils _utils;

    private static readonly Dictionary<string, string> CpuIds = new()
    {
        ["Ivy Bridge"] = "A9060300",
        ["Haswell"] = "C3060300",
        ["Broadwell"] = "D4060300",
        ["Coffee Lake"] = "EB060800",
        ["Comet Lake"] = "55060A00",
        ["Ice Lake"] = "E5060700"
    };

    public ConfigService(AppUtils utils)
    {
        _utils = utils;
    }

    public NSDictionary GenerateConfig(
        HardwareReport report,
        SmbiosData smbios,
        string macosVersion,
        HashSet<string> enabledKexts,
        List<string> acpiPatches,
        bool needsOclp = false)
    {
        var config = new NSDictionary
        {
            ["ACPI"] = GenerateAcpiSection(acpiPatches),
            ["Booter"] = GenerateBooterSection(report, smbios.SystemProductName, macosVersion),
            ["DeviceProperties"] = GenerateDevicePropertiesSection(report, macosVersion, enabledKexts),
            ["Kernel"] = GenerateKernelSection(report, macosVersion, enabledKexts, needsOclp),
            ["Misc"] = GenerateMiscSection(needsOclp),
            ["NVRAM"] = GenerateNvramSection(report, macosVersion, enabledKexts, needsOclp),
            ["PlatformInfo"] = GeneratePlatformInfoSection(smbios),
            ["UEFI"] = GenerateUefiSection(macosVersion)
        };

        return config;
    }

    private NSDictionary GenerateAcpiSection(List<string> acpiPatches)
    {
        var addArray = new NSArray();
        foreach (var patch in acpiPatches)
        {
            addArray.Add(Dict(
                ("Comment", Str(patch)),
                ("Enabled", Bool(true)),
                ("Path", Str($"{patch}.aml"))
            ));
        }

        return Dict(
            ("Add", addArray),
            ("Delete", EmptyArr()),
            ("Patch", EmptyArr()),
            ("Quirks", Dict(
                ("FadtEnableReset", Bool(false)),
                ("NormalizeHeaders", Bool(false)),
                ("RebaseRegions", Bool(false)),
                ("ResetHwSig", Bool(false)),
                ("ResetLogoStatus", Bool(false)),
                ("SyncTableIds", Bool(false))
            ))
        );
    }

    private NSDictionary GenerateBooterSection(HardwareReport report, string smbiosModel, string macosVersion)
    {
        var chipset = report.Motherboard?.Name ?? "";

        return Dict(
            ("MmioWhitelist", EmptyArr()),
            ("Patch", EmptyArr()),
            ("Quirks", Dict(
                ("AllowRelocationBlock", Bool(false)),
                ("AvoidRuntimeDefrag", Bool(true)),
                ("DevirtualiseMmio", Bool(ShouldDevirtualizeMmio(chipset))),
                ("DisableSingleUser", Bool(false)),
                ("DisableVariableWrite", Bool(false)),
                ("DiscardHibernateMap", Bool(false)),
                ("EnableSafeModeSlide", Bool(true)),
                ("EnableWriteUnprotector", Bool(!ShouldRebuildMemoryMap(report))),
                ("FixupAppleEfiImages", Bool(true)),
                ("ForceBooterSignature", Bool(false)),
                ("ForceExitBootServices", Bool(false)),
                ("ProtectMemoryRegions", Bool(false)),
                ("ProtectSecureBoot", Bool(false)),
                ("ProtectUefiServices", Bool(ShouldProtectUefiServices(chipset))),
                ("ProvideCustomSlide", Bool(true)),
                ("ProvideMaxSlide", Int(0)),
                ("RebuildAppleMemoryMap", Bool(ShouldRebuildMemoryMap(report))),
                ("ResizeAppleGpuBars", Int(-1)),
                ("SetupVirtualMap", Bool(ShouldSetupVirtualMap(report))),
                ("SignalAppleOS", Bool(false)),
                ("SyncRuntimePermissions", Bool(ShouldSyncRuntimePermissions(report)))
            ))
        );
    }

    private NSDictionary GenerateDevicePropertiesSection(
        HardwareReport report,
        string macosVersion,
        HashSet<string> enabledKexts)
    {
        var add = new NSDictionary();

        if (enabledKexts.Contains("WhateverGreen") && report.Gpus is not null)
        {
            foreach (var (_, gpu) in report.Gpus)
            {
                if (gpu.DeviceType == "Integrated GPU" && gpu.Manufacturer.Contains("Intel"))
                {
                    var igpuProps = GenerateIgpuProperties(gpu);
                    if (igpuProps.Count > 0)
                        add["PciRoot(0x0)/Pci(0x2,0x0)"] = igpuProps;
                }
            }
        }

        return Dict(
            ("Add", add),
            ("Delete", EmptyDict())
        );
    }

    private NSDictionary GenerateIgpuProperties(GpuInfo igpu)
    {
        var props = new NSDictionary();
        var deviceId = igpu.DeviceId.ToUpperInvariant();

        if (deviceId.StartsWith("3E") || deviceId.StartsWith("9B"))
        {
            props["AAPL,ig-platform-id"] = Data(_utils.HexToBytes("07009B3E"));
            props["framebuffer-patch-enable"] = Data(_utils.HexToBytes("01000000"));
            props["framebuffer-stolenmem"] = Data(_utils.HexToBytes("00003001"));
        }
        else if (deviceId.StartsWith("59"))
        {
            props["AAPL,ig-platform-id"] = Data(_utils.HexToBytes("00001259"));
            props["framebuffer-patch-enable"] = Data(_utils.HexToBytes("01000000"));
            props["framebuffer-stolenmem"] = Data(_utils.HexToBytes("00003001"));
            props["framebuffer-fbmem"] = Data(_utils.HexToBytes("00009000"));
        }
        else if (deviceId.StartsWith("8A"))
        {
            props["AAPL,ig-platform-id"] = Data(_utils.HexToBytes("0200518A"));
            props["device-id"] = Data(_utils.HexToBytes("528A0000"));
        }

        return props;
    }

    private NSDictionary GenerateKernelSection(
        HardwareReport report,
        string macosVersion,
        HashSet<string> enabledKexts,
        bool needsOclp)
    {
        var cpuManufacturer = report.Cpu?.Manufacturer ?? "";
        var isAmd = cpuManufacturer == "AuthenticAMD";

        return Dict(
            ("Add", EmptyArr()),
            ("Block", GenerateKernelBlock(enabledKexts)),
            ("Emulate", Dict(
                ("Cpuid1Data", EmptyData()),
                ("Cpuid1Mask", EmptyData()),
                ("DummyPowerManagement", Bool(isAmd)),
                ("MaxKernel", Str("")),
                ("MinKernel", Str(""))
            )),
            ("Force", EmptyArr()),
            ("Patch", EmptyArr()),
            ("Quirks", Dict(
                ("AppleCpuPmCfgLock", Bool(false)),
                ("AppleXcpmCfgLock", Bool(!isAmd)),
                ("AppleXcpmExtraMsrs", Bool(false)),
                ("AppleXcpmForceBoost", Bool(false)),
                ("CustomPciSerialDevice", Bool(false)),
                ("CustomSMBIOSGuid", Bool(true)),
                ("DisableIoMapper", Bool(!isAmd)),
                ("DisableIoMapperMapping", Bool(false)),
                ("DisableLinkeditJettison", Bool(true)),
                ("DisableRtcChecksum", Bool(false)),
                ("ExtendBTFeatureFlags", Bool(false)),
                ("ExternalDiskIcons", Bool(false)),
                ("ForceAquantiaEthernet", Bool(false)),
                ("ForceSecureBootScheme", Bool(false)),
                ("IncreasePciBarSize", Bool(false)),
                ("LapicKernelPanic", Bool(false)),
                ("LegacyCommpage", Bool(false)),
                ("PanicNoKextDump", Bool(true)),
                ("PowerTimeoutKernelPanic", Bool(true)),
                ("ProvideCurrentCpuInfo", Bool(isAmd)),
                ("SetApfsTrimTimeout", Int(-1)),
                ("ThirdPartyDrives", Bool(false)),
                ("XhciPortLimit", Bool(false))
            )),
            ("Scheme", Dict(
                ("CustomKernel", Bool(false)),
                ("FuzzyMatch", Bool(true)),
                ("KernelArch", Str("x86_64")),
                ("KernelCache", Str("Auto"))
            ))
        );
    }

    private NSArray GenerateKernelBlock(HashSet<string> enabledKexts)
    {
        var block = new NSArray();

        if (enabledKexts.Contains("IOSkywalkFamily"))
        {
            block.Add(Dict(
                ("Arch", Str("x86_64")),
                ("Comment", Str("Allow IOSkywalk Downgrade")),
                ("Enabled", Bool(true)),
                ("Identifier", Str("com.apple.iokit.IOSkywalkFamily")),
                ("MaxKernel", Str("")),
                ("MinKernel", Str("23.0.0")),
                ("Strategy", Str("Exclude"))
            ));
        }

        return block;
    }

    private NSDictionary GenerateMiscSection(bool needsOclp)
    {
        return Dict(
            ("BlessOverride", EmptyArr()),
            ("Boot", Dict(
                ("ConsoleAttributes", Int(0)),
                ("HibernateMode", Str("None")),
                ("HibernateSkipsPicker", Bool(true)),
                ("HideAuxiliary", Bool(true)),
                ("LauncherOption", Str("Full")),
                ("LauncherPath", Str("Default")),
                ("PickerAttributes", Int(17)),
                ("PickerAudioAssist", Bool(false)),
                ("PickerMode", Str("Builtin")),
                ("PickerVariant", Str("Auto")),
                ("PollAppleHotKeys", Bool(true)),
                ("ShowPicker", Bool(true)),
                ("TakeoffDelay", Int(0)),
                ("Timeout", Int(5))
            )),
            ("Debug", Dict(
                ("AppleDebug", Bool(true)),
                ("ApplePanic", Bool(true)),
                ("DisableWatchDog", Bool(true)),
                ("DisplayDelay", Int(0)),
                ("DisplayLevel", Long(2147483650)),
                ("LogModules", Str("*")),
                ("SysReport", Bool(false)),
                ("Target", Int(67))
            )),
            ("Entries", EmptyArr()),
            ("Security", Dict(
                ("AllowSetDefault", Bool(true)),
                ("ApECID", Int(0)),
                ("AuthRestart", Bool(false)),
                ("BlacklistAppleUpdate", Bool(true)),
                ("DmgLoading", Str("Signed")),
                ("EnablePassword", Bool(false)),
                ("ExposeSensitiveData", Int(6)),
                ("HaltLevel", Long(2147483648)),
                ("PasswordHash", EmptyData()),
                ("PasswordSalt", EmptyData()),
                ("ScanPolicy", Int(0)),
                ("SecureBootModel", Str(needsOclp ? "Disabled" : "Default")),
                ("Vault", Str("Optional"))
            )),
            ("Serial", Dict(
                ("Init", Bool(false)),
                ("Override", Bool(false))
            )),
            ("Tools", EmptyArr())
        );
    }

    private NSDictionary GenerateNvramSection(
        HardwareReport report,
        string macosVersion,
        HashSet<string> enabledKexts,
        bool needsOclp)
    {
        var bootArgs = GenerateBootArgs(report, macosVersion, enabledKexts, needsOclp);
        var csrConfig = GetCsrActiveConfig(macosVersion);

        return Dict(
            ("Add", Dict(
                ("4D1EDE05-38C7-4A6A-9CC6-4BCCA8B38C14", Dict(
                    ("DefaultBackgroundColor", Data([0x00, 0x00, 0x00, 0x00])),
                    ("UIScale", Data([0x01]))
                )),
                ("4D1FDA02-38C7-4A6A-9CC6-4BCCA8B30102", EmptyDict()),
                ("7C436110-AB2A-4BBB-A880-FE41995C9F82", Dict(
                    ("boot-args", Str(bootArgs)),
                    ("csr-active-config", Data(_utils.HexToBytes(csrConfig))),
                    ("prev-lang:kbd", Str("en-US:0")),
                    ("run-efi-updater", Str("No"))
                ))
            )),
            ("Delete", Dict(
                ("4D1EDE05-38C7-4A6A-9CC6-4BCCA8B38C14", EmptyArr()),
                ("4D1FDA02-38C7-4A6A-9CC6-4BCCA8B30102", EmptyArr()),
                ("7C436110-AB2A-4BBB-A880-FE41995C9F82", EmptyArr())
            )),
            ("LegacyOverwrite", Bool(false)),
            ("LegacySchema", EmptyDict()),
            ("WriteFlash", Bool(true))
        );
    }

    private string GenerateBootArgs(
        HardwareReport report,
        string macosVersion,
        HashSet<string> enabledKexts,
        bool needsOclp)
    {
        var args = new List<string> { "-v", "debug=0x100", "keepsyms=1" };
        var darwin = OsData.ParseDarwinVersion(macosVersion);

        if (needsOclp && darwin.Major >= 25)
            args.Add("-amfipassbeta");

        if (enabledKexts.Contains("WhateverGreen") && report.Gpus is not null)
        {
            var discreteGpu = report.Gpus.Values.FirstOrDefault(g => g.DeviceType == "Discrete GPU");
            if (discreteGpu is not null && discreteGpu.Codename.Contains("Navi"))
                args.Add("agdpmod=pikera");
        }

        if (enabledKexts.Contains("AppleALC") && report.Sound is not null)
        {
            var codec = report.Sound.Values.FirstOrDefault();
            if (codec is not null && CodecLayouts.Data.TryGetValue(codec.DeviceId, out var layouts))
            {
                var layout = layouts.FirstOrDefault();
                if (layout != default)
                    args.Add($"alcid={layout.Id}");
            }
        }

        return string.Join(" ", args);
    }

    private static string GetCsrActiveConfig(string macosVersion)
    {
        var darwin = OsData.ParseDarwinVersion(macosVersion);
        return darwin.Major switch
        {
            >= 20 => "030A0000",
            >= 18 => "FF070000",
            _ => "FF030000"
        };
    }

    private NSDictionary GeneratePlatformInfoSection(SmbiosData smbios)
    {
        return Dict(
            ("Automatic", Bool(true)),
            ("CustomMemory", Bool(false)),
            ("Generic", Dict(
                ("AdviseFeatures", Bool(false)),
                ("MaxBIOSVersion", Bool(false)),
                ("MLB", Str(smbios.MLB)),
                ("ProcessorType", Int(0)),
                ("ROM", Data(_utils.HexToBytes(smbios.ROM))),
                ("SpoofVendor", Bool(true)),
                ("SystemMemoryStatus", Str("Auto")),
                ("SystemProductName", Str(smbios.SystemProductName)),
                ("SystemSerialNumber", Str(smbios.SystemSerialNumber)),
                ("SystemUUID", Str(smbios.SystemUUID))
            )),
            ("UpdateDataHub", Bool(true)),
            ("UpdateNVRAM", Bool(true)),
            ("UpdateSMBIOS", Bool(true)),
            ("UpdateSMBIOSMode", Str("Custom")),
            ("UseRawUuidEncoding", Bool(false))
        );
    }

    private NSDictionary GenerateUefiSection(string macosVersion)
    {
        var darwin = OsData.ParseDarwinVersion(macosVersion);
        var drivers = new NSArray();
        var requiredDrivers = new List<string> { "OpenRuntime.efi", "HfsPlus.efi", "ResetNvramEntry.efi" };

        if (darwin.Major >= 25)
            requiredDrivers.Add("apfs_aligned.efi");

        foreach (var driver in requiredDrivers.OrderBy(d => d))
        {
            drivers.Add(Dict(
                ("Arguments", Str("")),
                ("Comment", Str("")),
                ("Enabled", Bool(true)),
                ("LoadEarly", Bool(false)),
                ("Path", Str(driver))
            ));
        }

        return Dict(
            ("APFS", Dict(
                ("EnableJumpstart", Bool(true)),
                ("GlobalConnect", Bool(false)),
                ("HideVerbose", Bool(true)),
                ("JumpstartHotPlug", Bool(false)),
                ("MinDate", Int(0)),
                ("MinVersion", Int(0))
            )),
            ("AppleInput", Dict(
                ("AppleEvent", Str("Builtin")),
                ("CustomDelays", Bool(false)),
                ("GraphicsInputMirroring", Bool(true)),
                ("KeyInitialDelay", Int(50)),
                ("KeySubsequentDelay", Int(5)),
                ("PointerDwellClickTimeout", Int(0)),
                ("PointerDwellDoubleClickTimeout", Int(0)),
                ("PointerDwellRadius", Int(0)),
                ("PointerPollMask", Int(-1)),
                ("PointerPollMax", Int(80)),
                ("PointerPollMin", Int(10)),
                ("PointerSpeedDiv", Int(1)),
                ("PointerSpeedMul", Int(1))
            )),
            ("Audio", Dict(
                ("AudioCodec", Int(0)),
                ("AudioDevice", Str("PciRoot(0x0)/Pci(0x1F,0x3)")),
                ("AudioOutMask", Int(-1)),
                ("AudioSupport", Bool(false)),
                ("DisconnectHda", Bool(false)),
                ("MaximumGain", Int(-15)),
                ("MinimumAssistGain", Int(-30)),
                ("MinimumAudibleGain", Int(-55)),
                ("PlayChime", Str("Auto")),
                ("ResetTrafficClass", Bool(false)),
                ("SetupDelay", Int(0))
            )),
            ("ConnectDrivers", Bool(true)),
            ("Drivers", drivers),
            ("Input", Dict(
                ("KeyFiltering", Bool(false)),
                ("KeyForgetThreshold", Int(5)),
                ("KeySupport", Bool(true)),
                ("KeySupportMode", Str("Auto")),
                ("KeySwap", Bool(false)),
                ("PointerSupport", Bool(false)),
                ("PointerSupportMode", Str("ASUS")),
                ("TimerResolution", Int(50000))
            )),
            ("Output", Dict(
                ("ClearScreenOnModeSwitch", Bool(false)),
                ("ConsoleFont", Str("")),
                ("ConsoleMode", Str("")),
                ("DirectGopRendering", Bool(false)),
                ("ForceResolution", Bool(false)),
                ("GopBurstMode", Bool(false)),
                ("GopPassThrough", Str("Disabled")),
                ("IgnoreTextInGraphics", Bool(false)),
                ("InitialMode", Str("Auto")),
                ("ProvideConsoleGop", Bool(true)),
                ("ReconnectGraphicsOnConnect", Bool(false)),
                ("ReconnectOnResChange", Bool(false)),
                ("ReplaceTabWithSpace", Bool(false)),
                ("Resolution", Str("Max")),
                ("SanitiseClearScreen", Bool(false)),
                ("TextRenderer", Str("BuiltinGraphics")),
                ("UIScale", Int(0)),
                ("UgaPassThrough", Bool(false))
            )),
            ("ProtocolOverrides", Dict(
                ("AppleAudio", Bool(false)),
                ("AppleBootPolicy", Bool(false)),
                ("AppleDebugLog", Bool(false)),
                ("AppleEg2Info", Bool(false)),
                ("AppleFramebufferInfo", Bool(false)),
                ("AppleImageConversion", Bool(false)),
                ("AppleImg4Verification", Bool(false)),
                ("AppleKeyMap", Bool(false)),
                ("AppleRtcRam", Bool(false)),
                ("AppleSecureBoot", Bool(false)),
                ("AppleSmcIo", Bool(false)),
                ("AppleUserInterfaceTheme", Bool(false)),
                ("DataHub", Bool(false)),
                ("DeviceProperties", Bool(false)),
                ("FirmwareVolume", Bool(false)),
                ("HashServices", Bool(false)),
                ("OSInfo", Bool(false)),
                ("PciIo", Bool(false)),
                ("UnicodeCollation", Bool(false))
            )),
            ("Quirks", Dict(
                ("ActivateHpetSupport", Bool(false)),
                ("DisableSecurityPolicy", Bool(false)),
                ("EnableVectorAcceleration", Bool(true)),
                ("EnableVmx", Bool(false)),
                ("ExitBootServicesDelay", Int(0)),
                ("ForceOcWriteFlash", Bool(false)),
                ("ForgeUefiSupport", Bool(false)),
                ("IgnoreInvalidFlexRatio", Bool(false)),
                ("ReloadOptionRoms", Bool(false)),
                ("RequestBootVarRouting", Bool(true)),
                ("ResizeGpuBars", Int(-1)),
                ("ResizeUsePciRbIo", Bool(false)),
                ("ShimRetainProtocol", Bool(false)),
                ("TscSyncTimeout", Int(0)),
                ("UnblockFsConnect", Bool(false))
            )),
            ("ReservedMemory", EmptyArr())
        );
    }

    // Helper methods
    private static bool ShouldDevirtualizeMmio(string chipset) =>
        chipset.Contains("Ice Lake") || chipset.Contains("B650") || chipset.Contains("X670") ||
        chipset.Contains("Z490") || chipset.Contains("Z590") || chipset.Contains("Z690") ||
        chipset.Contains("Z790");

    private static bool ShouldRebuildMemoryMap(HardwareReport report) =>
        report.Cpu?.Manufacturer == "AuthenticAMD" ||
        report.Motherboard?.Name?.Contains("Z490") == true ||
        report.Motherboard?.Name?.Contains("Z590") == true;

    private static bool ShouldProtectUefiServices(string chipset) =>
        chipset.Contains("Z490") || chipset.Contains("Z590") ||
        chipset.Contains("Z690") || chipset.Contains("Z790");

    private static bool ShouldSetupVirtualMap(HardwareReport report) =>
        report.Cpu?.Manufacturer != "AuthenticAMD";

    private static bool ShouldSyncRuntimePermissions(HardwareReport report) =>
        report.Cpu?.Manufacturer == "AuthenticAMD" ||
        report.Motherboard?.Name?.Contains("Z490") == true;

    public async Task SaveConfigAsync(NSDictionary config, string outputPath, CancellationToken ct = default)
    {
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        PropertyListParser.SaveAsXml(config, stream);
    }
}
