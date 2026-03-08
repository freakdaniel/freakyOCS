using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.Services;

/// <summary>
/// Validates hardware report JSON against a schema, checking required fields,
/// patterns, and types. Ported from report_validator.py.
/// </summary>
public sealed class ReportValidatorService
{
    private readonly ILogger<ReportValidatorService>? _logger;
    private List<string> _errors = [];
    private List<string> _warnings = [];

    private static readonly Dictionary<string, string> Patterns = new()
    {
        ["not_empty"] = @".+",
        ["platform"] = @"^(Desktop|Laptop)$",
        ["firmware_type"] = @"^(UEFI|BIOS)$",
        ["bus_type"] = @"^(PCI|USB|ACPI|ROOT)$",
        ["cpu_manufacturer"] = @"^(Intel|AMD)$",
        ["gpu_manufacturer"] = @"^(Intel|AMD|NVIDIA)$",
        ["gpu_device_type"] = @"^(Integrated GPU|Discrete GPU|Unknown)$",
        ["hex_id"] = @"^(?:0x)?[0-9a-fA-F]+$",
        ["device_id"] = @"^[0-9A-F]{4}(?:-[0-9A-F]{4})?$",
        ["resolution"] = @"^\d+x\d+$",
        ["pci_path"] = @"^PciRoot\(0x[0-9a-fA-F]+\)(?:/Pci\(0x[0-9a-fA-F]+,0x[0-9a-fA-F]+\))+$",
        ["acpi_path"] = @"^[\\]?_SB(\.[A-Z0-9_]+)+$",
        ["core_count"] = @"^\d+$",
        ["connector_type"] = @"^(VGA|DVI|HDMI|LVDS|DP|eDP|Internal|Uninitialized)$",
        ["enabled_disabled"] = @"^(Enabled|Disabled)$",
    };

    public ReportValidatorService(ILogger<ReportValidatorService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a hardware report JSON. Returns (isValid, errors, warnings, cleanedJson).
    /// </summary>
    public (bool IsValid, List<string> Errors, List<string> Warnings, JsonElement? CleanedData)
        ValidateReport(string reportJson)
    {
        _errors = [];
        _warnings = [];

        try
        {
            var doc = JsonDocument.Parse(reportJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                _errors.Add("Root: Expected object, got " + root.ValueKind);
                return (false, _errors, _warnings, null);
            }

            ValidateSection(root, "Motherboard", true, new()
            {
                ["Name"] = (true, null),
                ["Chipset"] = (true, null),
                ["Platform"] = (true, "platform"),
            });

            ValidateSection(root, "BIOS", true, new()
            {
                ["Version"] = (false, null),
                ["Release Date"] = (false, null),
                ["System Type"] = (false, null),
                ["Firmware Type"] = (true, "firmware_type"),
                ["Secure Boot"] = (true, "enabled_disabled"),
            });

            ValidateSection(root, "CPU", true, new()
            {
                ["Manufacturer"] = (true, "cpu_manufacturer"),
                ["Processor Name"] = (true, null),
                ["Codename"] = (true, null),
                ["Core Count"] = (true, "core_count"),
                ["CPU Count"] = (true, "core_count"),
                ["SIMD Features"] = (true, null),
            });

            ValidateDeviceSection(root, "GPU", true, new()
            {
                ["Manufacturer"] = (true, "gpu_manufacturer"),
                ["Codename"] = (true, null),
                ["Device ID"] = (true, "device_id"),
                ["Device Type"] = (true, "gpu_device_type"),
                ["Subsystem ID"] = (false, "hex_id"),
                ["PCI Path"] = (false, "pci_path"),
                ["ACPI Path"] = (false, "acpi_path"),
                ["Resizable BAR"] = (false, "enabled_disabled"),
            });

            ValidateDeviceSection(root, "Network", true, new()
            {
                ["Bus Type"] = (true, "bus_type"),
                ["Device ID"] = (true, "device_id"),
                ["Subsystem ID"] = (false, "hex_id"),
                ["PCI Path"] = (false, "pci_path"),
                ["ACPI Path"] = (false, "acpi_path"),
            });

            ValidateDeviceSection(root, "Sound", false, new()
            {
                ["Bus Type"] = (true, null),
                ["Device ID"] = (true, "device_id"),
                ["Subsystem ID"] = (false, "hex_id"),
            });

            ValidateDeviceSection(root, "USB Controllers", true, new()
            {
                ["Bus Type"] = (true, "bus_type"),
                ["Device ID"] = (true, "device_id"),
                ["Subsystem ID"] = (false, "hex_id"),
                ["PCI Path"] = (false, "pci_path"),
                ["ACPI Path"] = (false, "acpi_path"),
            });

            ValidateDeviceSection(root, "Storage Controllers", true, new()
            {
                ["Bus Type"] = (true, "bus_type"),
                ["Device ID"] = (true, "device_id"),
                ["Subsystem ID"] = (false, "hex_id"),
                ["PCI Path"] = (false, "pci_path"),
                ["ACPI Path"] = (false, "acpi_path"),
            });

            ValidateDeviceSection(root, "Biometric", false, new()
            {
                ["Bus Type"] = (true, "bus_type"),
                ["Device ID"] = (false, "device_id"),
            });

            ValidateDeviceSection(root, "Bluetooth", false, new()
            {
                ["Bus Type"] = (true, "bus_type"),
                ["Device ID"] = (true, "device_id"),
            });

            var isValid = _errors.Count == 0;
            return (isValid, _errors, _warnings, root);
        }
        catch (JsonException ex)
        {
            _errors.Add($"Invalid JSON format: {ex.Message}");
            return (false, _errors, _warnings, null);
        }
    }

    /// <summary>
    /// Validates a hardware report from a file path.
    /// </summary>
    public (bool IsValid, List<string> Errors, List<string> Warnings, JsonElement? CleanedData)
        ValidateReportFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return (false, [$"File does not exist: {filePath}"], [], null);
        }

        var json = File.ReadAllText(filePath);
        return ValidateReport(json);
    }

    private void ValidateSection(JsonElement root, string sectionName, bool required,
        Dictionary<string, (bool Required, string? Pattern)> fields)
    {
        if (!root.TryGetProperty(sectionName, out var section))
        {
            if (required)
                _errors.Add($"Root: Missing required key '{sectionName}'");
            return;
        }

        if (section.ValueKind != JsonValueKind.Object)
        {
            _errors.Add($"{sectionName}: Expected object, got {section.ValueKind}");
            return;
        }

        foreach (var (fieldName, (fieldRequired, pattern)) in fields)
        {
            if (!section.TryGetProperty(fieldName, out var field))
            {
                if (fieldRequired)
                    _errors.Add($"{sectionName}: Missing required key '{fieldName}'");
                continue;
            }

            if (field.ValueKind == JsonValueKind.String)
            {
                var value = field.GetString() ?? "";
                if (pattern is not null && !Regex.IsMatch(value, Patterns[pattern]))
                    _errors.Add($"{sectionName}.{fieldName}: Value '{value}' does not match pattern '{Patterns[pattern]}'");
                else if (string.IsNullOrEmpty(value) && fieldRequired)
                    _errors.Add($"{sectionName}.{fieldName}: Value is empty");
            }
        }
    }

    private void ValidateDeviceSection(JsonElement root, string sectionName, bool required,
        Dictionary<string, (bool Required, string? Pattern)> fields)
    {
        if (!root.TryGetProperty(sectionName, out var section))
        {
            if (required)
                _errors.Add($"Root: Missing required key '{sectionName}'");
            return;
        }

        if (section.ValueKind != JsonValueKind.Object)
        {
            _errors.Add($"{sectionName}: Expected object, got {section.ValueKind}");
            return;
        }

        foreach (var deviceProp in section.EnumerateObject())
        {
            var deviceName = deviceProp.Name;
            var device = deviceProp.Value;

            if (device.ValueKind != JsonValueKind.Object)
            {
                _errors.Add($"{sectionName}.{deviceName}: Expected object, got {device.ValueKind}");
                continue;
            }

            foreach (var (fieldName, (fieldRequired, pattern)) in fields)
            {
                if (!device.TryGetProperty(fieldName, out var field))
                {
                    if (fieldRequired)
                        _errors.Add($"{sectionName}.{deviceName}: Missing required key '{fieldName}'");
                    continue;
                }

                if (field.ValueKind == JsonValueKind.String)
                {
                    var value = field.GetString() ?? "";
                    if (pattern is not null && !Regex.IsMatch(value, Patterns[pattern]))
                        _errors.Add($"{sectionName}.{deviceName}.{fieldName}: Value '{value}' does not match pattern '{Patterns[pattern]}'");
                }
            }
        }
    }
}
