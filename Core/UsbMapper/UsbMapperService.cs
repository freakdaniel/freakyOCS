using System.Runtime.InteropServices;
using System.Text.Json;
using Claunia.PropertyList;
using Microsoft.Extensions.Logging;
using static OcsNet.Core.Services.PlistHelper;

namespace OcsNet.Core.UsbMapper;

/// <summary>
/// Cross-platform USB port mapping service.
/// Discovers USB controllers and ports, allows selection, and generates kext.
/// </summary>
public sealed partial class UsbMapperService
{
    private readonly ILogger<UsbMapperService>? _logger;

    private List<UsbController>? _controllers;
    private List<UsbController>? _controllersHistorical;
    private UsbMapperSettings _settings = new();

    private readonly string _dataPath;
    private readonly string _settingsPath;

    public UsbMapperService(string workingDirectory, ILogger<UsbMapperService>? logger = null)
    {
        _logger = logger;
        _dataPath = Path.Combine(workingDirectory, "usb.json");
        _settingsPath = Path.Combine(workingDirectory, "usb_settings.json");

        LoadSettings();
        LoadHistoricalData();
    }

    public UsbMapperSettings Settings => _settings;
    public List<UsbController>? Controllers => _controllers;
    public List<UsbController>? ControllersHistorical => _controllersHistorical;
    public bool HasSavedData => File.Exists(_dataPath);

    /// <summary>
    /// Gets USB controllers and their ports.
    /// </summary>
    public async Task<List<UsbController>> GetControllersAsync(CancellationToken ct = default)
    {
        List<UsbController> controllers;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            controllers = await GetWindowsControllersAsync(ct);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            controllers = await GetLinuxControllersAsync(ct);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            controllers = await GetMacOSControllersAsync(ct);
        }
        else
        {
            controllers = [];
        }

        _controllers = controllers;

        if (_controllersHistorical is null)
        {
            _controllersHistorical = DeepCopy(controllers);
        }
        else
        {
            MergeControllers(_controllersHistorical, controllers);
        }

        return controllers;
    }

    /// <summary>
    /// Updates device information for current controllers.
    /// </summary>
    public async Task UpdateDevicesAsync(CancellationToken ct = default)
    {
        await GetControllersAsync(ct);
    }

    /// <summary>
    /// Selects or deselects a port by index.
    /// </summary>
    public void TogglePort(int selectionIndex)
    {
        if (_controllersHistorical is null) return;

        var port = GetPortBySelectionIndex(selectionIndex);
        if (port is null) return;

        port.Selected = !port.Selected;

        if (_settings.AutoBindCompanions)
        {
            var companion = GetCompanionPort(port);
            if (companion is not null)
            {
                companion.Selected = port.Selected;
            }
        }
    }

    /// <summary>
    /// Sets connector type for a port.
    /// </summary>
    public void SetPortType(int selectionIndex, UsbConnectorType connectorType)
    {
        if (_controllersHistorical is null) return;

        var port = GetPortBySelectionIndex(selectionIndex);
        if (port is null) return;

        port.ConnectorType = connectorType;

        if (_settings.AutoBindCompanions)
        {
            var companion = GetCompanionPort(port);
            if (companion is not null)
            {
                companion.ConnectorType = connectorType;
            }
        }
    }

    /// <summary>
    /// Sets comment for a port.
    /// </summary>
    public void SetPortComment(int selectionIndex, string? comment)
    {
        var port = GetPortBySelectionIndex(selectionIndex);
        if (port is not null)
        {
            port.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;
        }
    }

    /// <summary>
    /// Selects all ports.
    /// </summary>
    public void SelectAllPorts()
    {
        if (_controllersHistorical is null) return;
        foreach (var controller in _controllersHistorical)
        {
            foreach (var port in controller.Ports)
            {
                port.Selected = true;
            }
        }
    }

    /// <summary>
    /// Deselects all ports.
    /// </summary>
    public void DeselectAllPorts()
    {
        if (_controllersHistorical is null) return;
        foreach (var controller in _controllersHistorical)
        {
            foreach (var port in controller.Ports)
            {
                port.Selected = false;
            }
        }
    }

    /// <summary>
    /// Enables all populated ports (ports with devices connected at some point).
    /// </summary>
    public void EnablePopulatedPorts()
    {
        if (_controllersHistorical is null) return;
        foreach (var controller in _controllersHistorical)
        {
            foreach (var port in controller.Ports)
            {
                if (port.Devices.Count > 0 || GetCompanionPort(port)?.Devices.Count > 0)
                {
                    port.Selected = true;
                }
            }
        }
    }

    /// <summary>
    /// Disables all empty ports.
    /// </summary>
    public void DisableEmptyPorts()
    {
        if (_controllersHistorical is null) return;
        foreach (var controller in _controllersHistorical)
        {
            foreach (var port in controller.Ports)
            {
                if (port.Devices.Count == 0 && (GetCompanionPort(port)?.Devices.Count ?? 0) == 0)
                {
                    port.Selected = false;
                }
            }
        }
    }

    /// <summary>
    /// Validates port selections before building kext.
    /// </summary>
    public List<string> ValidateSelections()
    {
        var errors = new List<string>();

        if (_controllersHistorical is null || !_controllersHistorical.Any(c => c.Ports.Any(p => p.Selected)))
        {
            errors.Add("No ports are selected! Select some ports.");
            return errors;
        }

        foreach (var controller in _controllersHistorical)
        {
            var selectedCount = controller.Ports.Count(p => p.Selected);
            if (selectedCount > 15)
            {
                errors.Add($"Controller {controller.Name} has {selectedCount} ports selected. macOS supports maximum 15 ports per controller.");
            }

            foreach (var port in controller.Ports.Where(p => p.Selected))
            {
                if (port.ConnectorType is null && port.GuessedType is null)
                {
                    errors.Add($"Port {port.SelectionIndex} ({port.Name}) is missing a connector type!");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Builds the USB map kext.
    /// </summary>
    public async Task<string?> BuildKextAsync(string outputDirectory, string? modelIdentifier = null, CancellationToken ct = default)
    {
        if (_controllersHistorical is null)
            return null;

        var errors = ValidateSelections();
        if (errors.Count > 0)
        {
            _logger?.LogError("Validation failed: {Errors}", string.Join(", ", errors));
            return null;
        }

        // Determine kext name based on settings
        string kextName;
        if (_settings.UseNativeClasses && _settings.UseLegacyNativeClasses)
            kextName = "USBMapLegacy.kext";
        else if (_settings.UseNativeClasses)
            kextName = "USBMap.kext";
        else
            kextName = "UTBMap.kext";

        var kextPath = Path.Combine(outputDirectory, kextName);
        var contentsPath = Path.Combine(kextPath, "Contents");

        // Remove existing kext
        if (Directory.Exists(kextPath))
        {
            Directory.Delete(kextPath, recursive: true);
        }

        Directory.CreateDirectory(contentsPath);

        // Build Info.plist
        var plist = BuildInfoPlist(modelIdentifier);

        // Write plist
        var plistPath = Path.Combine(contentsPath, "Info.plist");
        await using var stream = new FileStream(plistPath, FileMode.Create);
        PropertyListParser.SaveAsXml(plist, stream);

        _logger?.LogInformation("USB map kext saved to {Path}", kextPath);
        return kextPath;
    }

    private NSDictionary BuildInfoPlist(string? modelIdentifier)
    {
        var personalities = new NSDictionary();

        foreach (var controller in _controllersHistorical!)
        {
            if (!controller.Ports.Any(p => p.Selected))
                continue;

            var personalityName = GetPersonalityName(controller);
            var personality = BuildControllerPersonality(controller, modelIdentifier);
            personalities[personalityName] = personality;
        }

        var plist = new NSDictionary
        {
            ["CFBundleDevelopmentRegion"] = Str("English"),
            ["CFBundleIdentifier"] = Str(_settings.UseNativeClasses
                ? "com.apple.driver.AppleUSBHostMergeProperties"
                : "com.USBToolBox.kext.UTBMap"),
            ["CFBundleInfoDictionaryVersion"] = Str("6.0"),
            ["CFBundleName"] = Str("UTBMap"),
            ["CFBundlePackageType"] = Str("KEXT"),
            ["CFBundleShortVersionString"] = Str("1.0.0"),
            ["CFBundleVersion"] = Str("1.0.0"),
            ["IOKitPersonalities"] = personalities
        };

        if (!_settings.UseNativeClasses)
        {
            plist["OSBundleLibraries"] = Dict(
                ("com.dhinakg.USBToolBox.kext", Str("1.0.0"))
            );
        }

        return plist;
    }

    private NSDictionary BuildControllerPersonality(UsbController controller, string? modelIdentifier)
    {
        NSDictionary personality;

        if (_settings.UseNativeClasses)
        {
            var ioClass = _settings.UseLegacyNativeClasses ? "AppleUSBMergeNub" : "AppleUSBHostMergeProperties";
            personality = new NSDictionary
            {
                ["CFBundleIdentifier"] = Str($"com.apple.driver.{ioClass}"),
                ["IOClass"] = Str(ioClass),
                ["IOProviderClass"] = Str("AppleUSBHostController"),
                ["IOParentMatch"] = ChooseMatchingKey(controller)
            };

            if (modelIdentifier is not null)
            {
                personality["model"] = Str(modelIdentifier);
            }
        }
        else
        {
            personality = new NSDictionary
            {
                ["CFBundleIdentifier"] = Str("com.dhinakg.USBToolBox.kext"),
                ["IOClass"] = Str("USBToolBox"),
                ["IOProviderClass"] = Str("IOPCIDevice"),
                ["IOMatchCategory"] = Str("USBToolBox")
            };

            foreach (var (key, value) in ChooseMatchingKey(controller))
            {
                personality[key] = value;
            }
        }

        // Build ports dictionary
        var ports = new NSDictionary();
        var portNameIndex = new Dictionary<string, int>();
        var highestIndex = 0;

        foreach (var port in controller.Ports.Where(p => p.Selected).OrderBy(p => p.Index))
        {
            if (port.Index > highestIndex)
                highestIndex = port.Index;

            var prefix = controller.ControllerType == UsbControllerType.XHCI
                ? port.SpeedClass == UsbDeviceSpeed.SuperSpeed || port.SpeedClass == UsbDeviceSpeed.SuperSpeedPlus
                    ? "SS"
                    : "HS"
                : "PRT";

            if (!portNameIndex.TryGetValue(prefix, out var idx))
            {
                idx = 1;
            }

            var portName = $"{prefix}{idx:D2}";
            portNameIndex[prefix] = idx + 1;

            var portDict = new NSDictionary
            {
                ["port"] = Data(IntToLittleEndianBytes(port.Index)),
                ["UsbConnector"] = Int((int)(port.ConnectorType ?? port.GuessedType ?? UsbConnectorType.USB3TypeA))
            };

            if (_settings.AddCommentsToMap && port.Comment is not null)
            {
                portDict["#comment"] = Str(port.Comment);
            }

            ports[portName] = portDict;
        }

        personality["IOProviderMergeProperties"] = Dict(
            ("ports", ports),
            ("port-count", Data(IntToLittleEndianBytes(highestIndex)))
        );

        return personality;
    }

    private NSDictionary ChooseMatchingKey(UsbController controller)
    {
        var id = controller.Identifiers;

        // M1 Macs - use bus-number
        if (id.BusNumber.HasValue)
        {
            return Dict(
                ("IOPropertyMatch", Dict(
                    ("bus-number", Data(IntToLittleEndianBytes(id.BusNumber.Value)))
                ))
            );
        }

        // Use ACPI path if unique
        if (!string.IsNullOrEmpty(id.AcpiPath))
        {
            var acpiName = id.AcpiPath.Contains('.') ? id.AcpiPath.Split('.').Last() : id.AcpiPath;
            return Dict(("IONameMatch", Str(acpiName)));
        }

        // Use BDF (bus-device-function)
        if (id.Bdf is not null && id.Bdf.Length == 3)
        {
            return Dict(
                ("IOPropertyMatch", Dict(
                    ("pcidebug", Str($"{id.Bdf[0]}:{id.Bdf[1]}:{id.Bdf[2]}"))
                ))
            );
        }

        // Use PCI ID
        if (id.PciId is not null && id.PciId.Length >= 2)
        {
            var match = new NSDictionary
            {
                ["IOPCIPrimaryMatch"] = Str($"0x{id.PciId[1]}{id.PciId[0]}")
            };

            if (id.PciId.Length >= 4)
            {
                match["IOPCISecondaryMatch"] = Str($"0x{id.PciId[3]}{id.PciId[2]}");
            }

            return match;
        }

        // Fallback to path
        if (!string.IsNullOrEmpty(id.Path))
        {
            return Dict(("IOPathMatch", Str(id.Path)));
        }

        throw new InvalidOperationException("No matching key available for controller");
    }

    private static string GetPersonalityName(UsbController controller)
    {
        var id = controller.Identifiers;

        if (!string.IsNullOrEmpty(id.AcpiPath))
        {
            return id.AcpiPath.Contains('.') ? id.AcpiPath.Split('.').Last() : id.AcpiPath;
        }

        if (id.Bdf is not null && id.Bdf.Length == 3)
        {
            return $"{id.Bdf[0]}:{id.Bdf[1]}:{id.Bdf[2]}";
        }

        return controller.Name;
    }

    private UsbPort? GetPortBySelectionIndex(int index)
    {
        if (_controllersHistorical is null) return null;

        foreach (var controller in _controllersHistorical)
        {
            var port = controller.Ports.FirstOrDefault(p => p.SelectionIndex == index);
            if (port is not null) return port;
        }

        return null;
    }

    private UsbPort? GetCompanionPort(UsbPort port)
    {
        if (_controllersHistorical is null || port.CompanionInfo is null)
            return null;

        var hubName = port.CompanionInfo.Hub;
        var portIndex = port.CompanionInfo.Port;

        if (hubName is null || portIndex is null)
            return null;

        var hub = _controllersHistorical.FirstOrDefault(c => c.HubName == hubName);
        return hub?.Ports.FirstOrDefault(p => p.Index == portIndex);
    }

    private static byte[] IntToLittleEndianBytes(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    private static T DeepCopy<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    // Load/Save methods
    private void LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            var json = File.ReadAllText(_settingsPath);
            _settings = JsonSerializer.Deserialize<UsbMapperSettings>(json) ?? new();
        }
    }

    public void SaveSettings()
    {
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    private void LoadHistoricalData()
    {
        if (File.Exists(_dataPath))
        {
            var json = File.ReadAllText(_dataPath);
            _controllersHistorical = JsonSerializer.Deserialize<List<UsbController>>(json);
        }
    }

    public void SaveHistoricalData()
    {
        if (_controllersHistorical is not null)
        {
            var json = JsonSerializer.Serialize(_controllersHistorical, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataPath, json);
        }
    }

    public void ClearHistoricalData()
    {
        _controllers = null;
        _controllersHistorical = null;

        if (File.Exists(_dataPath))
        {
            File.Delete(_dataPath);
        }
    }

    /// <summary>
    /// Assigns selection indices to all ports.
    /// </summary>
    public void AssignSelectionIndices()
    {
        if (_controllersHistorical is null) return;

        int index = 1;
        foreach (var controller in _controllersHistorical)
        {
            controller.SelectedCount = 0;
            foreach (var port in controller.Ports)
            {
                port.SelectionIndex = index++;

                if (!port.Selected && port.Devices.Count > 0)
                {
                    port.Selected = true;
                }

                if (port.Selected)
                {
                    controller.SelectedCount++;
                }
            }
        }
    }
}
