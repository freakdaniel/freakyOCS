using OcsNet.Core.Datasets;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.Services;

/// <summary>
/// Service responsible for ACPI table analysis and SSDT generation.
/// Generates SSDTs for CPU power management, system clock, EC, IRQ conflicts,
/// USB power, device disabling, and more.
/// Ported from acpi_guru.py.
/// </summary>
public sealed class AcpiGuruService
{
    private readonly DsdtService _dsdt;
    private readonly AppUtils _utils;
    private readonly ProcessRunner _runner;
    private readonly ILogger<AcpiGuruService>? _logger;

    private string _acpiDirectory = "";

    private static readonly Dictionary<string, string> OsiStrings = new()
    {
        ["Windows 2000"] = "Windows 2000",
        ["Windows XP"] = "Windows 2001",
        ["Windows Vista"] = "Windows 2006",
        ["Windows 7"] = "Windows 2009",
        ["Windows 8"] = "Windows 2012",
        ["Windows 8.1"] = "Windows 2013",
        ["Windows 10"] = "Windows 2015",
        ["Windows 11"] = "Windows 2021",
        ["Windows 11, version 22H2"] = "Windows 2022",
    };

    private static readonly (string PrePatch, string Comment, string Find, string Replace)[] PrePatches =
    [
        ("GPP7 duplicate _PRW methods", "GPP7._PRW to XPRW to fix Gigabyte's Mistake",
            "3708584847500A021406535245470214065350525701085F505257",
            "3708584847500A0214065352454702140653505257010858505257"),
        ("GPP7 duplicate UP00 devices", "GPP7.UP00 to UPXX to fix Gigabyte's Mistake",
            "1047052F035F53425F50434930475050375B82450455503030",
            "1047052F035F53425F50434930475050375B82450455505858"),
        ("GPP6 duplicate _PRW methods", "GPP6._PRW to XPRW to fix ASRock's Mistake",
            "47505036085F4144520C04000200140F5F505257",
            "47505036085F4144520C04000200140F58505257"),
        ("GPP1 duplicate PTXH devices", "GPP1.PTXH to XTXH to fix MSI's Mistake",
            "50545848085F41445200140F",
            "58545848085F41445200140F"),
    ];

    public AcpiGuruService(
        DsdtService dsdt,
        AppUtils utils,
        ProcessRunner runner,
        ILogger<AcpiGuruService>? logger = null)
    {
        _dsdt = dsdt;
        _utils = utils;
        _runner = runner;
        _logger = logger;
    }

    /// <summary>
    /// Generates all required SSDTs based on hardware report and selected patches.
    /// Returns (generatedSsdtFiles, acpiPatches).
    /// </summary>
    public async Task<AcpiGenerationResult> GenerateAcpiPatchesAsync(
        HardwareReport report,
        string smbiosModel,
        string acpiDir,
        List<string> selectedPatches,
        Dictionary<string, object>? disabledDevices = null,
        CancellationToken ct = default)
    {
        _acpiDirectory = acpiDir;
        Directory.CreateDirectory(_acpiDirectory);

        var result = new AcpiGenerationResult();
        var cpuCodename = report.Cpu?.Codename ?? "";
        var cpuManufacturer = report.Cpu?.Manufacturer ?? "";
        var platform = report.Motherboard?.Platform ?? "Desktop";
        var isLaptop = platform == "Laptop";
        var isIntel = cpuManufacturer.Contains("Intel");

        // Always-needed SSDTs
        if (selectedPatches.Contains("SSDT-PLUG") || selectedPatches.Contains("enable_cpu_power_management"))
        {
            var ssdt = GenerateSsdtPlug(cpuCodename, isIntel);
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-PLUG", ssdt, ct));
        }

        if (selectedPatches.Contains("SSDT-EC") || selectedPatches.Contains("fake_embedded_controller"))
        {
            var ssdt = GenerateSsdtEc(isLaptop);
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-EC", ssdt, ct));
        }

        if (selectedPatches.Contains("SSDT-RTCAWAC") || selectedPatches.Contains("fix_system_clock_awac"))
        {
            var ssdt = GenerateSsdtRtcAwac();
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-RTCAWAC", ssdt, ct));
        }

        if (selectedPatches.Contains("SSDT-USBX") || selectedPatches.Contains("add_usb_power_properties"))
        {
            var ssdt = GenerateSsdtUsbx(smbiosModel);
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-USBX", ssdt, ct));
        }

        if (selectedPatches.Contains("SSDT-HPET") || selectedPatches.Contains("fix_irq_conflicts"))
        {
            var ssdt = GenerateSsdtHpet();
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-HPET", ssdt, ct));
        }

        if ((selectedPatches.Contains("SSDT-IMEI") || selectedPatches.Contains("add_intel_management_engine")) && isIntel)
        {
            var ssdt = GenerateSsdtImei();
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-IMEI", ssdt, ct));
        }

        if (selectedPatches.Contains("SSDT-SBUS") || selectedPatches.Contains("add_system_management_bus_device"))
        {
            var ssdt = GenerateSsdtSbus();
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-SBUS", ssdt, ct));
        }

        if (selectedPatches.Contains("SSDT-MCHC") || selectedPatches.Contains("add_memory_controller_device"))
        {
            var ssdt = GenerateSsdtMchc();
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-MCHC", ssdt, ct));
        }

        if (isLaptop && (selectedPatches.Contains("SSDT-ALS0") || selectedPatches.Contains("ambient_light_sensor")))
        {
            var ssdt = GenerateSsdtAls0();
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-ALS0", ssdt, ct));
        }

        if (isLaptop && (selectedPatches.Contains("SSDT-PNLF") || selectedPatches.Contains("fix_backlight")))
        {
            var ssdt = GenerateSsdtPnlf();
            if (ssdt is not null)
                result.SsdtFiles.Add(await CompileSsdtAsync("SSDT-PNLF", ssdt, ct));
        }

        // Handle disabled device SSDTs
        if (disabledDevices is not null)
        {
            foreach (var (name, _) in disabledDevices)
            {
                if (name.Contains("GPU"))
                {
                    var ssdt = GenerateDisableGpuSsdt(name);
                    if (ssdt is not null)
                        result.SsdtFiles.Add(await CompileSsdtAsync($"SSDT-Disable_{name.Replace(":", "_")}", ssdt, ct));
                }
            }
        }

        result.SsdtFiles.RemoveAll(f => f is null);
        return result;
    }

    private string GenerateSsdtPlug(string cpuCodename, bool isIntel)
    {
        if (!isIntel)
        {
            // AMD doesn't need SSDT-PLUG
            return null!;
        }

        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "CpuPlug", 0x00003000)
{
    External (_SB_.PR00, ProcessorObj)
    Scope (_SB_.PR00)
    {
        If (_OSI ("Darwin")) {
            Method (_DSM, 4, NotSerialized)
            {
                If (LNot (Arg2))
                {
                    Return (Buffer (One)
                    {
                        0x03
                    })
                }
                Return (Package (0x02)
                {
                    "plugin-type", 
                    One
                })
            }
        }
    }
}
""";
    }

    private string GenerateSsdtEc(bool isLaptop)
    {
        var lpcName = "_SB.PCI0.LPCB";

        if (isLaptop)
        {
            return $$"""
DefinitionBlock ("", "SSDT", 2, "ZPSS", "EC", 0x00001000)
{
    External ({{lpcName}}, DeviceObj)
    Scope ({{lpcName}})
    {
        Device (EC)
        {
            Name (_HID, "ACID0001")
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
        }

        return $$"""
DefinitionBlock ("", "SSDT", 2, "ZPSS", "EC", 0x00001000)
{
    External ({{lpcName}}, DeviceObj)
    Scope ({{lpcName}})
    {
        Device (EC)
        {
            Name (_HID, "ACID0001")
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
    }

    private string? GenerateSsdtRtcAwac()
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "RTCAWAC", 0x00000000)
{
    External (STAS, IntObj)
    Scope (\)
    {
        Method (_INI, 0, NotSerialized)
        {
            If (_OSI ("Darwin"))
            {
                Store (One, STAS)
            }
        }
    }
}
""";
    }

    private string? GenerateSsdtUsbx(string smbiosModel)
    {
        string props;
        if (smbiosModel.Contains("MacPro") || smbiosModel.Contains("iMacPro") ||
            smbiosModel.Contains("iMac"))
        {
            props = """
                    "kUSBSleepPowerSupply",
                    0x13EC,
                    "kUSBSleepPortCurrentLimit",
                    0x0834,
                    "kUSBWakePowerSupply",
                    0x13EC,
                    "kUSBWakePortCurrentLimit",
                    0x0834
                    """;
        }
        else if (smbiosModel.Contains("MacBookPro"))
        {
            props = """
                    "kUSBSleepPortCurrentLimit",
                    0x0BB8,
                    "kUSBWakePortCurrentLimit",
                    0x0BB8
                    """;
        }
        else
        {
            props = """
                    "kUSBSleepPowerSupply",
                    0x0C80,
                    "kUSBSleepPortCurrentLimit",
                    0x0834,
                    "kUSBWakePowerSupply",
                    0x0C80,
                    "kUSBWakePortCurrentLimit",
                    0x0834
                    """;
        }

        return $$"""
DefinitionBlock ("", "SSDT", 2, "ZPSS", "USBX", 0x00001000)
{
    Scope (\_SB)
    {
        Device (USBX)
        {
            Name (_ADR, Zero)
            Method (_DSM, 4, NotSerialized)
            {
                If (LNot (Arg2))
                {
                    Return (Buffer ()
                    {
                        0x03
                    })
                }
                Return (Package ()
                {
{{props}}
                })
            }
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
    }

    private string? GenerateSsdtHpet()
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "HPET", 0x00000000)
{
    External (_SB.PCI0.LPCB, DeviceObj)
    Scope (_SB.PCI0.LPCB)
    {
        Device (HPET)
        {
            Name (_HID, EisaId ("PNP0103"))
            Name (_CID, EisaId ("PNP0C01"))
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
            Name (_CRS, ResourceTemplate ()
            {
                IRQNoFlags ()
                    {0,8,11}
                Memory32Fixed (ReadWrite,
                    0xFED00000,
                    0x00000400,
                    )
            })
        }
    }
}
""";
    }

    private string? GenerateSsdtImei()
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "IMEI", 0x00000000)
{
    External (_SB_.PCI0, DeviceObj)
    Scope (_SB.PCI0)
    {
        Device (IMEI)
        {
            Name (_ADR, 0x00160000)
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
    }

    private string? GenerateSsdtSbus()
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "SBUS", 0)
{
    External (_SB.PCI0.SBUS, DeviceObj)
    Scope (_SB.PCI0.SBUS)
    {
        Device (BUS0)
        {
            Name (_CID, "smbus")
            Name (_ADR, Zero)
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
    }

    private string? GenerateSsdtMchc()
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "MCHC", 0)
{
    External (_SB.PCI0, DeviceObj)
    Scope (_SB.PCI0)
    {
        Device (MCHC)
        {
            Name (_ADR, Zero)
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
    }

    private string? GenerateSsdtAls0()
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "ALS0", 0x00000000)
{
    Scope (_SB)
    {
        Device (ALS0)
        {
            Name (_HID, "ACPI0008")
            Name (_CID, "smc-als")
            Name (_ALI, 0x012C)
            Name (_ALR, Package (0x01)
            {
                Package (0x02)
                {
                    0x64, 
                    0x012C
                }
            })
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0F)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
    }

    private string? GenerateSsdtPnlf()
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "PNLF", 0x00000000)
{
    External (_SB.PCI0.GFX0, DeviceObj)
    Scope (_SB.PCI0.GFX0)
    {
        Device (PNLF)
        {
            Name (_HID, EisaId ("APP0002"))
            Name (_CID, "backlight")
            Name (_UID, 0x13)
            Method (_STA, 0, NotSerialized)
            {
                If (_OSI ("Darwin"))
                {
                    Return (0x0B)
                }
                Else
                {
                    Return (Zero)
                }
            }
        }
    }
}
""";
    }

    private string? GenerateDisableGpuSsdt(string deviceName)
    {
        return """
DefinitionBlock ("", "SSDT", 2, "ZPSS", "DGPU", 0x00000000)
{
    Device (DGPU)
    {
        Name (_HID, "DGPU1000")
        Method (_STA, 0, NotSerialized)
        {
            If (_OSI ("Darwin"))
            {
                Return (0x0F)
            }
            Else
            {
                Return (Zero)
            }
        }
    }
}
""";
    }

    private async Task<SsdtFile?> CompileSsdtAsync(string name, string dslContent, CancellationToken ct)
    {
        try
        {
            var dslPath = Path.Combine(_acpiDirectory, $"{name}.dsl");
            var amlPath = Path.Combine(_acpiDirectory, $"{name}.aml");

            await File.WriteAllTextAsync(dslPath, dslContent, ct);

            var iaslAvailable = await _dsdt.EnsureIaslAvailableAsync(ct);
            if (!iaslAvailable)
            {
                _logger?.LogWarning("iasl not available, keeping DSL file: {Name}", name);
                return new SsdtFile(name, dslPath, false);
            }

            var result = await _runner.RunAsync("iasl", [dslPath], cancellationToken: ct);
            if (result.Success && File.Exists(amlPath))
            {
                // Remove DSL after successful compilation
                File.Delete(dslPath);
                return new SsdtFile(name, amlPath, true);
            }

            _logger?.LogWarning("Failed to compile {Name}: {Output}", name, result.Output);
            return new SsdtFile(name, dslPath, false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error compiling SSDT {Name}", name);
            return null;
        }
    }
}

public class AcpiGenerationResult
{
    public List<SsdtFile?> SsdtFiles { get; set; } = [];
    public List<AcpiPatchEntry> Patches { get; set; } = [];
}

public record SsdtFile(string Name, string Path, bool Compiled);

public record AcpiPatchEntry(
    string Comment,
    string Find,
    string Replace,
    bool Enabled = true);
