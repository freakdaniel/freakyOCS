namespace OcsNet.Core.Datasets;

public sealed record GithubRepo(string Owner, string Repo);

public sealed record DownloadInfo(int Id, string Url, string? Sha256 = null);

public sealed record KextInfo(
    string Name,
    string Description,
    string Category,
    bool Required = false,
    string? MinDarwinVersion = null,
    string? MaxDarwinVersion = null,
    string[]? RequiresKexts = null,
    string? ConflictGroupId = null,
    GithubRepo? GithubRepo = null,
    DownloadInfo? DownloadInfo = null
)
{
    public string EffectiveMin => MinDarwinVersion ?? OsData.GetLowestDarwinVersion();
    public string EffectiveMax => MaxDarwinVersion ?? OsData.GetLatestDarwinVersion();
    public bool Checked => Required;
}

public static class KextData
{
    public static readonly KextInfo[] Kexts =
    [
        new(
            Name: "Lilu",
            Description: "For arbitrary kext, library, and program patching",
            Category: "Required",
            Required: true,
            GithubRepo: new("acidanthera", "Lilu")
        ),
        new(
            Name: "VirtualSMC",
            Description: "Advanced Apple SMC emulator in the kernel",
            Category: "Required",
            Required: true,
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "VirtualSMC")
        ),
        new(
            Name: "SMCBatteryManager",
            Description: "Manages, monitors, and reports on battery status",
            Category: "VirtualSMC Plugins",
            RequiresKexts: ["Lilu", "VirtualSMC"],
            GithubRepo: new("acidanthera", "VirtualSMC")
        ),
        new(
            Name: "SMCDellSensors",
            Description: "Enables fan monitoring and control on Dell computers",
            Category: "VirtualSMC Plugins",
            RequiresKexts: ["Lilu", "VirtualSMC"],
            GithubRepo: new("acidanthera", "VirtualSMC")
        ),
        new(
            Name: "SMCLightSensor",
            Description: "Allows system utilize ambient light sensor device",
            Category: "VirtualSMC Plugins",
            RequiresKexts: ["Lilu", "VirtualSMC"],
            GithubRepo: new("acidanthera", "VirtualSMC")
        ),
        new(
            Name: "SMCProcessor",
            Description: "Manages Intel CPU temperature sensors",
            Category: "VirtualSMC Plugins",
            RequiresKexts: ["Lilu", "VirtualSMC"],
            GithubRepo: new("acidanthera", "VirtualSMC")
        ),
        new(
            Name: "SMCRadeonSensors",
            Description: "Provides temperature readings for AMD GPUs",
            Category: "VirtualSMC Plugins",
            MinDarwinVersion: "18.0.0",
            RequiresKexts: ["Lilu", "VirtualSMC"],
            GithubRepo: new("ChefKissInc", "SMCRadeonSensors"),
            DownloadInfo: new(0, "https://nightly.link/ChefKissInc/SMCRadeonSensors/workflows/main/master/Artifacts.zip")
        ),
        new(
            Name: "SMCSuperIO",
            Description: "Monitoring hardware sensors and controlling fan speeds",
            Category: "VirtualSMC Plugins",
            RequiresKexts: ["Lilu", "VirtualSMC"],
            GithubRepo: new("acidanthera", "VirtualSMC")
        ),
        new(
            Name: "NootRX",
            Description: "The rDNA 2 dGPU support patch kext",
            Category: "Graphics",
            MinDarwinVersion: "20.5.0",
            RequiresKexts: ["Lilu"],
            ConflictGroupId: "GPU",
            GithubRepo: new("ChefKissInc", "NootRX"),
            DownloadInfo: new(0, "https://nightly.link/ChefKissInc/NootRX/workflows/main/master/Artifacts.zip")
        ),
        new(
            Name: "NootedRed",
            Description: "The AMD Vega iGPU support kext",
            Category: "Graphics",
            MinDarwinVersion: "19.0.0",
            RequiresKexts: ["Lilu"],
            ConflictGroupId: "GPU",
            GithubRepo: new("ChefKissInc", "NootedRed"),
            DownloadInfo: new(0, "https://nightly.link/ChefKissInc/NootedRed/workflows/main/master/Artifacts.zip")
        ),
        new(
            Name: "WhateverGreen",
            Description: "Various patches necessary for GPUs are pre-supported",
            Category: "Graphics",
            RequiresKexts: ["Lilu"],
            ConflictGroupId: "GPU",
            GithubRepo: new("acidanthera", "WhateverGreen")
        ),
        new(
            Name: "AppleALC",
            Description: "Native macOS HD audio for not officially supported codecs",
            Category: "Audio",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "AppleALC")
        ),
        new(
            Name: "AirportBrcmFixup",
            Description: "Patches required for non-native Broadcom Wi-Fi cards",
            Category: "Wi-Fi",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "AirportBrcmFixup")
        ),
        new(
            Name: "AirportItlwm",
            Description: "Intel Wi-Fi drivers support the native macOS Wi-Fi interface",
            Category: "Wi-Fi",
            ConflictGroupId: "IntelWiFi",
            GithubRepo: new("OpenIntelWireless", "itlwm")
        ),
        new(
            Name: "corecaptureElCap",
            Description: "Enable legacy Qualcomm Atheros Wireless cards",
            Category: "Wi-Fi",
            MinDarwinVersion: "18.0.0",
            MaxDarwinVersion: "24.99.99",
            RequiresKexts: ["IO80211ElCap"],
            DownloadInfo: new(348147192, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/refs/heads/main/payloads/Kexts/Wifi/corecaptureElCap-v1.0.2.zip")
        ),
        new(
            Name: "IO80211ElCap",
            Description: "Enable legacy Qualcomm Atheros Wireless cards",
            Category: "Wi-Fi",
            MinDarwinVersion: "18.0.0",
            MaxDarwinVersion: "24.99.99",
            RequiresKexts: ["corecaptureElCap"],
            DownloadInfo: new(128321732, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/refs/heads/main/payloads/Kexts/Wifi/IO80211ElCap-v2.0.1.zip")
        ),
        new(
            Name: "IO80211FamilyLegacy",
            Description: "Enable legacy Apple Wireless adapters",
            Category: "Wi-Fi",
            MinDarwinVersion: "23.0.0",
            RequiresKexts: ["AMFIPass", "IOSkywalkFamily"],
            DownloadInfo: new(817294638, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/main/payloads/Kexts/Wifi/IO80211FamilyLegacy-v1.0.0.zip")
        ),
        new(
            Name: "IOSkywalkFamily",
            Description: "Enable legacy Apple Wireless adapters",
            Category: "Wi-Fi",
            MinDarwinVersion: "23.0.0",
            RequiresKexts: ["AMFIPass", "IO80211FamilyLegacy"],
            DownloadInfo: new(926584761, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/main/payloads/Kexts/Wifi/IOSkywalkFamily-v1.2.0.zip")
        ),
        new(
            Name: "itlwm",
            Description: "Intel Wi-Fi drivers. Spoofs as Ethernet and connects to Wi-Fi via Heliport",
            Category: "Wi-Fi",
            ConflictGroupId: "IntelWiFi",
            GithubRepo: new("OpenIntelWireless", "itlwm")
        ),
        new(
            Name: "Ath3kBT",
            Description: "Uploads firmware to enable Atheros Bluetooth support",
            Category: "Bluetooth",
            MaxDarwinVersion: "20.99.99",
            RequiresKexts: ["Ath3kBTInjector"],
            GithubRepo: new("zxystd", "AthBluetoothFirmware")
        ),
        new(
            Name: "Ath3kBTInjector",
            Description: "Uploads firmware to enable Atheros Bluetooth support",
            Category: "Bluetooth",
            MaxDarwinVersion: "20.99.99",
            RequiresKexts: ["Ath3kBT"],
            GithubRepo: new("zxystd", "AthBluetoothFirmware")
        ),
        new(
            Name: "BlueToolFixup",
            Description: "Patches Bluetooth stack to support third-party cards",
            Category: "Bluetooth",
            MinDarwinVersion: "21.0.0",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "BrcmPatchRAM")
        ),
        new(
            Name: "BrcmBluetoothInjector",
            Description: "Enables the Broadcom Bluetooth on/off switch on older versions",
            Category: "Bluetooth",
            MaxDarwinVersion: "20.99.99",
            RequiresKexts: ["BrcmBluetoothInjector", "BrcmFirmwareData", "BrcmPatchRAM2", "BrcmPatchRAM3"],
            GithubRepo: new("acidanthera", "BrcmPatchRAM")
        ),
        new(
            Name: "BrcmFirmwareData",
            Description: "Applies PatchRAM updates for Broadcom RAMUSB based devices",
            Category: "Bluetooth",
            RequiresKexts: ["BlueToolFixup", "BrcmBluetoothInjector", "BrcmPatchRAM2", "BrcmPatchRAM3"],
            GithubRepo: new("acidanthera", "BrcmPatchRAM")
        ),
        new(
            Name: "BrcmPatchRAM2",
            Description: "Applies PatchRAM updates for Broadcom RAMUSB based devices",
            Category: "Bluetooth",
            MaxDarwinVersion: "18.99.99",
            RequiresKexts: ["BlueToolFixup", "BrcmBluetoothInjector", "BrcmFirmwareData", "BrcmPatchRAM3"],
            GithubRepo: new("acidanthera", "BrcmPatchRAM")
        ),
        new(
            Name: "BrcmPatchRAM3",
            Description: "Applies PatchRAM updates for Broadcom RAMUSB based devices",
            Category: "Bluetooth",
            MinDarwinVersion: "19.0.0",
            RequiresKexts: ["BlueToolFixup", "BrcmBluetoothInjector", "BrcmFirmwareData", "BrcmPatchRAM2"],
            GithubRepo: new("acidanthera", "BrcmPatchRAM")
        ),
        new(
            Name: "IntelBluetoothFirmware",
            Description: "Uploads firmware to enable Intel Bluetooth support",
            Category: "Bluetooth",
            RequiresKexts: ["BlueToolFixup", "IntelBTPatcher", "IntelBluetoothInjector"],
            GithubRepo: new("lshbluesky", "IntelBluetoothFirmware")
        ),
        new(
            Name: "IntelBTPatcher",
            Description: "Fixes Intel Bluetooth bugs for better connectivity",
            Category: "Bluetooth",
            RequiresKexts: ["Lilu", "BlueToolFixup", "IntelBluetoothFirmware", "IntelBluetoothInjector"],
            GithubRepo: new("lshbluesky", "IntelBluetoothFirmware")
        ),
        new(
            Name: "IntelBluetoothInjector",
            Description: "Enables the Intel Bluetooth on/off switch on older versions",
            Category: "Bluetooth",
            MaxDarwinVersion: "20.99.99",
            RequiresKexts: ["BlueToolFixup", "IntelBluetoothFirmware", "IntelBTPatcher"],
            GithubRepo: new("lshbluesky", "IntelBluetoothFirmware")
        ),
        new(
            Name: "AppleIGB",
            Description: "Provides support for Intel's IGB Ethernet controllers",
            Category: "Ethernet",
            GithubRepo: new("donatengit", "AppleIGB"),
            DownloadInfo: new(736194363, "https://github.com/lzhoang2801/lzhoang2801.github.io/raw/main/public/extra-files/AppleIGB-v5.11.4.zip")
        ),
        new(
            Name: "AppleIGC",
            Description: "Provides support for Intel 2.5G Ethernet(i225/i226)",
            Category: "Ethernet",
            GithubRepo: new("SongXiaoXi", "AppleIGC")
        ),
        new(
            Name: "AtherosE2200Ethernet",
            Description: "Provides support for Atheros E2200 family",
            Category: "Ethernet",
            GithubRepo: new("Mieze", "AtherosE2200Ethernet")
        ),
        new(
            Name: "CatalinaBCM5701Ethernet",
            Description: "Provides support for Broadcom BCM57XX Ethernet series",
            Category: "Ethernet",
            MinDarwinVersion: "20.0.0",
            DownloadInfo: new(821327912, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/refs/heads/main/payloads/Kexts/Ethernet/CatalinaBCM5701Ethernet-v1.0.2.zip")
        ),
        new(
            Name: "HoRNDIS",
            Description: "Use the USB tethering mode of the Android phone to access the Internet",
            Category: "Ethernet",
            GithubRepo: new("TomHeaven", "HoRNDIS"),
            DownloadInfo: new(79378595, "https://github.com/TomHeaven/HoRNDIS/releases/download/rel9.3_2/Release.zip")
        ),
        new(
            Name: "IntelLucy",
            Description: "Provides support for Intel X500 family",
            Category: "Ethernet",
            GithubRepo: new("Mieze", "IntelLucy")
        ),
        new(
            Name: "IntelMausiEthernet",
            Description: "Intel Ethernet LAN driver for macOS",
            Category: "Ethernet",
            GithubRepo: new("CloverHackyColor", "IntelMausiEthernet")
        ),
        new(
            Name: "LucyRTL8125Ethernet",
            Description: "Provides support for Realtek RTL8125 family",
            Category: "Ethernet",
            GithubRepo: new("Mieze", "LucyRTL8125Ethernet")
        ),
        new(
            Name: "NullEthernet",
            Description: "Creates a Null Ethernet when no supported network hardware is present",
            Category: "Ethernet",
            GithubRepo: new("RehabMan", "os-x-null-ethernet"),
            DownloadInfo: new(182736492, "https://bitbucket.org/RehabMan/os-x-null-ethernet/downloads/RehabMan-NullEthernet-2016-1220.zip")
        ),
        new(
            Name: "RealtekRTL8100",
            Description: "Provides support for Realtek RTL8100 family",
            Category: "Ethernet",
            GithubRepo: new("Mieze", "RealtekRTL8100"),
            DownloadInfo: new(10460478, "https://github.com/lzhoang2801/lzhoang2801.github.io/raw/main/public/extra-files/RealtekRTL8100-v2.0.1.zip")
        ),
        new(
            Name: "RealtekRTL8111",
            Description: "Provides support for Realtek RTL8111/8168 family",
            Category: "Ethernet",
            GithubRepo: new("Mieze", "RTL8111_driver_for_OS_X"),
            DownloadInfo: new(130015132, "https://github.com/Mieze/RTL8111_driver_for_OS_X/releases/download/2.4.2/RealtekRTL8111-V2.4.2.zip")
        ),
        new(
            Name: "GenericUSBXHCI",
            Description: "Fixes USB 3.0 issues found on some Ryzen APU-based",
            Category: "USB",
            GithubRepo: new("RattletraPM", "GUX-RyzenXHCIFix")
        ),
        new(
            Name: "USBToolBox",
            Description: "Flexible USB mapping",
            Category: "USB",
            GithubRepo: new("USBToolBox", "kext")
        ),
        new(
            Name: "UTBDefault",
            Description: "Enables all USB ports (assumes no port limit)",
            Category: "USB",
            RequiresKexts: ["USBToolBox"],
            GithubRepo: new("USBToolBox", "kext")
        ),
        new(
            Name: "XHCI-unsupported",
            Description: "Enables USB 3.0 support for unsupported xHCI controllers",
            Category: "USB",
            GithubRepo: new("daliansky", "OS-X-USB-Inject-All"),
            DownloadInfo: new(185465401, "https://github.com/daliansky/OS-X-USB-Inject-All/releases/download/v0.8.0/XHCI-unsupported.kext.zip")
        ),
        new(
            Name: "AlpsHID",
            Description: "Brings native multitouch support to the Alps I2C touchpad",
            Category: "Input",
            RequiresKexts: ["VoodooI2C"],
            GithubRepo: new("blankmac", "AlpsHID")
        ),
        new(
            Name: "VoodooInput",
            Description: "Provides Magic Trackpad 2 software emulation for arbitrary input sources",
            Category: "Input",
            GithubRepo: new("acidanthera", "VoodooInput")
        ),
        new(
            Name: "VoodooPS2Controller",
            Description: "Provides support for PS/2 keyboards, trackpads, and mouse",
            Category: "Input",
            GithubRepo: new("acidanthera", "VoodooPS2")
        ),
        new(
            Name: "VoodooRMI",
            Description: "Synaptic Trackpad kext over SMBus/I2C",
            Category: "Input",
            GithubRepo: new("VoodooSMBus", "VoodooRMI")
        ),
        new(
            Name: "VoodooSMBus",
            Description: "i2c-i801 + ELAN SMBus Touchpad kext",
            Category: "Input",
            MinDarwinVersion: "18.0.0",
            GithubRepo: new("VoodooSMBus", "VoodooSMBus")
        ),
        new(
            Name: "VoodooI2C",
            Description: "Intel I2C controller and slave device drivers",
            Category: "Input",
            GithubRepo: new("VoodooI2C", "VoodooI2C")
        ),
        new(
            Name: "VoodooI2CAtmelMXT",
            Description: "A satellite kext for Atmel MXT I2C touchscreen",
            Category: "Input",
            RequiresKexts: ["VoodooI2C"],
            GithubRepo: new("VoodooI2C", "VoodooI2C")
        ),
        new(
            Name: "VoodooI2CELAN",
            Description: "A satellite kext for ELAN I2C touchpads",
            Category: "Input",
            RequiresKexts: ["VoodooI2C"],
            GithubRepo: new("VoodooI2C", "VoodooI2C")
        ),
        new(
            Name: "VoodooI2CFTE",
            Description: "A satellite kext for FTE based touchpads",
            Category: "Input",
            RequiresKexts: ["VoodooI2C"],
            GithubRepo: new("VoodooI2C", "VoodooI2C")
        ),
        new(
            Name: "VoodooI2CHID",
            Description: "A satellite kext for HID I2C or ELAN1200+ input devices",
            Category: "Input",
            RequiresKexts: ["VoodooI2C"],
            GithubRepo: new("VoodooI2C", "VoodooI2C")
        ),
        new(
            Name: "VoodooI2CSynaptics",
            Description: "A satellite kext for Synaptics I2C touchpads",
            Category: "Input",
            RequiresKexts: ["VoodooI2C"],
            GithubRepo: new("VoodooI2C", "VoodooI2C")
        ),
        new(
            Name: "AsusSMC",
            Description: "Supports ALS, keyboard backlight, and Fn keys on ASUS laptops",
            Category: "Brand Specific",
            MaxDarwinVersion: "23.99.99",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("hieplpvip", "AsusSMC")
        ),
        new(
            Name: "BigSurface",
            Description: "A fully intergrated kext for all Surface related hardwares",
            Category: "Brand Specific",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("Xiashangning", "BigSurface")
        ),
        new(
            Name: "YogaSMC",
            Description: "Enables support for syncing SMC keys, controlling sensors and managing vendor-specific features",
            Category: "Brand Specific",
            RequiresKexts: ["Lilu", "VirtualSMC"],
            GithubRepo: new("zhen-zen", "YogaSMC")
        ),
        new(
            Name: "CtlnaAHCIPort",
            Description: "Improves support for certain SATA controllers",
            Category: "Storage",
            MinDarwinVersion: "20.0.0",
            ConflictGroupId: "SATA",
            DownloadInfo: new(927362352, "https://raw.githubusercontent.com/lzhoang2801/lzhoang2801.github.io/refs/heads/main/public/extra-files/CtlnaAHCIPort-v3.4.1.zip",
                "c8cf54f8b98995d076f365765025068e3d612f6337e279774203441c06f1a474")
        ),
        new(
            Name: "SATA-unsupported",
            Description: "Improves support for certain SATA controllers",
            Category: "Storage",
            MaxDarwinVersion: "19.99.99",
            ConflictGroupId: "SATA",
            DownloadInfo: new(239471623, "https://raw.githubusercontent.com/lzhoang2801/lzhoang2801.github.io/refs/heads/main/public/extra-files/SATA-unsupported-v0.9.2.zip",
                "942395056afa1e1d0e06fb501ab7c0130bf687d00e08b02c271844769056a57c")
        ),
        new(
            Name: "NVMeFix",
            Description: "Addresses compatibility and performance issues with NVMe SSDs",
            Category: "Storage",
            MinDarwinVersion: "18.0.0",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "NVMeFix")
        ),
        new(
            Name: "RealtekCardReader",
            Description: "Realtek PCIe/USB-based SD card reader driver",
            Category: "Card Reader",
            MinDarwinVersion: "18.0.0",
            MaxDarwinVersion: "23.99.99",
            RequiresKexts: ["RealtekCardReaderFriend"],
            ConflictGroupId: "RealtekCardReader",
            GithubRepo: new("0xFireWolf", "RealtekCardReader")
        ),
        new(
            Name: "RealtekCardReaderFriend",
            Description: "Makes System Information recognize your Realtek card reader",
            Category: "Card Reader",
            MinDarwinVersion: "18.0.0",
            MaxDarwinVersion: "22.99.99",
            RequiresKexts: ["Lilu", "RealtekCardReader"],
            GithubRepo: new("0xFireWolf", "RealtekCardReaderFriend")
        ),
        new(
            Name: "Sinetek-rtsx",
            Description: "Realtek PCIe-based SD card reader driver",
            Category: "Card Reader",
            ConflictGroupId: "RealtekCardReader",
            GithubRepo: new("cholonam", "Sinetek-rtsx")
        ),
        new(
            Name: "AmdTscSync",
            Description: "A modified version of VoodooTSCSync for AMD CPUs",
            Category: "TSC Synchronization",
            ConflictGroupId: "TSC",
            GithubRepo: new("naveenkrdy", "AmdTscSync")
        ),
        new(
            Name: "VoodooTSCSync",
            Description: "A kernel extension which will synchronize the TSC on Intel CPUs",
            Category: "TSC Synchronization",
            ConflictGroupId: "TSC",
            GithubRepo: new("RehabMan", "VoodooTSCSync"),
            DownloadInfo: new(823728912, "https://github.com/lzhoang2801/lzhoang2801.github.io/raw/refs/heads/main/public/extra-files/VoodooTSCSync-v1.1.zip")
        ),
        new(
            Name: "CpuTscSync",
            Description: "Lilu plugin for TSC sync and disabling xcpm_urgency on Intel CPUs",
            Category: "TSC Synchronization",
            RequiresKexts: ["Lilu"],
            ConflictGroupId: "TSC",
            GithubRepo: new("acidanthera", "CpuTscSync")
        ),
        new(
            Name: "ForgedInvariant",
            Description: "The plug & play kext for syncing the TSC on AMD & Intel",
            Category: "TSC Synchronization",
            RequiresKexts: ["Lilu"],
            ConflictGroupId: "TSC",
            GithubRepo: new("ChefKissInc", "ForgedInvariant"),
            DownloadInfo: new(0, "https://nightly.link/ChefKissInc/ForgedInvariant/workflows/main/master/Artifacts.zip")
        ),
        new(
            Name: "AMFIPass",
            Description: "A replacement for amfi=0x80 boot argument",
            Category: "Extras",
            MinDarwinVersion: "20.0.0",
            RequiresKexts: ["Lilu"],
            DownloadInfo: new(926491527, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/main/payloads/Kexts/Acidanthera/AMFIPass-v1.4.1-RELEASE.zip")
        ),
        new(
            Name: "ASPP-Override",
            Description: "Re-enable CPU power management for Intel Sandy Bridge CPUs",
            Category: "Extras",
            MinDarwinVersion: "21.4.0",
            DownloadInfo: new(913826421, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/refs/heads/main/payloads/Kexts/Misc/ASPP-Override-v1.0.1.zip")
        ),
        new(
            Name: "AppleIntelCPUPowerManagement",
            Description: "Re-enable CPU power management on legacy Intel CPUs",
            Category: "Extras",
            MinDarwinVersion: "22.0.0",
            DownloadInfo: new(736296452, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/refs/heads/main/payloads/Kexts/Misc/AppleIntelCPUPowerManagement-v1.0.0.zip")
        ),
        new(
            Name: "AppleIntelCPUPowerManagementClient",
            Description: "Re-enable CPU power management on legacy Intel CPUs",
            Category: "Extras",
            MinDarwinVersion: "22.0.0",
            DownloadInfo: new(932639706, "https://github.com/dortania/OpenCore-Legacy-Patcher/raw/refs/heads/main/payloads/Kexts/Misc/AppleIntelCPUPowerManagementClient-v1.0.0.zip")
        ),
        new(
            Name: "AppleMCEReporterDisabler",
            Description: "Disables AppleMCEReporter.kext to prevent kernel panics",
            Category: "Extras",
            DownloadInfo: new(738162736, "https://github.com/acidanthera/bugtracker/files/3703498/AppleMCEReporterDisabler.kext.zip")
        ),
        new(
            Name: "BrightnessKeys",
            Description: "Handler for brightness keys without DSDT patches",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "BrightnessKeys")
        ),
        new(
            Name: "CPUFriend",
            Description: "Dynamic power management data injection (requires CPUFriendDataProvider)",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "CPUFriend")
        ),
        new(
            Name: "CpuTopologyRebuild",
            Description: "Optimizes the core configuration of Intel Alder Lake CPUs+",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("b00t0x", "CpuTopologyRebuild")
        ),
        new(
            Name: "CryptexFixup",
            Description: "Various patches to install Rosetta cryptex",
            Category: "Extras",
            MinDarwinVersion: "22.0.0",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "CryptexFixup")
        ),
        new(
            Name: "ECEnabler",
            Description: "Allows reading Embedded Controller fields over 1 byte long",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("1Revenger1", "ECEnabler")
        ),
        new(
            Name: "FeatureUnlock",
            Description: "Enable additional features on unsupported hardware",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "FeatureUnlock")
        ),
        new(
            Name: "HibernationFixup",
            Description: "Fixes hibernation compatibility issues",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "HibernationFixup")
        ),
        new(
            Name: "NoTouchID",
            Description: "Avoid lag in authentication dialogs for board IDs with Touch ID sensors",
            Category: "Extras",
            MinDarwinVersion: "17.5.0",
            MaxDarwinVersion: "19.6.0",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("al3xtjames", "NoTouchID")
        ),
        new(
            Name: "RestrictEvents",
            Description: "Blocking unwanted processes and unlocking features",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "RestrictEvents")
        ),
        new(
            Name: "RTCMemoryFixup",
            Description: "Emulate some offsets in your CMOS (RTC) memory",
            Category: "Extras",
            RequiresKexts: ["Lilu"],
            GithubRepo: new("acidanthera", "RTCMemoryFixup")
        ),
    ];

    public static KextInfo? GetByName(string name) =>
        Kexts.FirstOrDefault(k => k.Name == name);
}
