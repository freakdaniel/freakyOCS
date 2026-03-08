namespace OcsNet.Core.Datasets;

public sealed record AcpiPatchInfo(string Name, string Description, string FunctionName);

public static class AcpiPatchData
{
    public static readonly AcpiPatchInfo[] Patches =
    [
        new("ALS",            "Fake or enable Ambient Light Sensor device for storing the current brightness/auto-brightness level", "ambient_light_sensor"),
        new("APIC",           "Avoid kernel panic by pointing the first CPU entry to an active CPU on HEDT systems",                "fix_apic_processor_id"),
        new("BATP",           "Enables displaying the battery percentage on laptops",                                               "battery_status_patch"),
        new("BUS0",           "Add a System Management Bus device to fix AppleSMBus issues",                                       "add_system_management_bus_device"),
        new("Disable Devices","Disable unsupported PCI devices such as the GPU, Wi-Fi card, and SD card reader",                   "disable_unsupported_device"),
        new("FakeEC",         "OS-Aware Fake EC (by CorpNewt)",                                                                    "fake_embedded_controller"),
        new("RCSP",           "Remove conditional ACPI scope declaration",                                                         "remove_conditional_scope"),
        new("CMOS",           "Fix HP Real-Time Clock Power Loss (005) Post Error",                                                "fix_hp_005_post_error"),
        new("FixHPET",        "Patch Out IRQ Conflicts (by CorpNewt)",                                                             "fix_irq_conflicts"),
        new("GPI0",           "Enable GPIO device for a I2C TouchPads to function properly",                                       "enable_gpio_device"),
        new("IMEI",           "Creates a fake IMEI device to ensure Intel iGPUs acceleration functions properly",                  "add_intel_management_engine"),
        new("MCHC",           "Add a Memory Controller Hub Controller device to fix AppleSMBus",                                   "add_memory_controller_device"),
        new("PMC",            "Add a PMCR device to enable NVRAM support for 300-series mainboards",                               "enable_nvram_support"),
        new("PM (Legacy)",    "Block CpuPm and Cpu0Ist ACPI tables to avoid panics for Intel Ivy Bridge and older CPUs",           "drop_cpu_tables"),
        new("PLUG",           "Redefines CPU Objects as Processor and sets plugin-type = 1 (by CorpNewt)",                         "enable_cpu_power_management"),
        new("PNLF",           "Defines a PNLF device to enable backlight controls on laptops",                                     "enable_backlight_controls"),
        new("RMNE",           "Creates a Null Ethernet to allow macOS system access to iServices",                                  "add_null_ethernet_device"),
        new("RTC0",           "Creates a new RTC device to resolve PCI Configuration issues on HEDT systems",                      "fix_system_clock_hedt"),
        new("RTCAWAC",        "Context-Aware AWAC Disable and RTC Enable/Fake/Range Fix (by CorpNewt)",                            "fix_system_clock_awac"),
        new("PRW",            "Fix sleep state values in _PRW methods to prevent immediate wake in macOS",                          "instant_wake_fix"),
        new("Surface Patch",  "Special Patch for all Surface Pro / Book / Laptop hardwares",                                       "surface_laptop_special_patch"),
        new("UNC",            "Disables unused uncore bridges to prevent kenel panic on HEDT systems",                             "fix_uncore_bridge"),
        new("USB Reset",      "Disable USB Hub devices to manually rebuild the ports",                                             "disable_usb_hub_devices"),
        new("USBX",           "Creates an USBX device to inject USB power properties",                                             "add_usb_power_properties"),
        new("WMIS",           "Certain models forget to return result from ThermalZone",                                           "return_thermal_zone"),
        new("XOSI",           "Spoofs the operating system to Windows, enabling devices locked behind non-Windows systems on macOS","operating_system_patch"),
    ];
}
