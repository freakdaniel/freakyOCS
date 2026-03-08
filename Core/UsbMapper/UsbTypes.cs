using System.Text.Json.Serialization;

namespace OcsNet.Core.UsbMapper;

/// <summary>
/// USB controller types based on interface class.
/// </summary>
public enum UsbControllerType
{
    Unknown = 0x00,
    OHCI = 0x10,    // Open Host Controller Interface (USB 1.1)
    UHCI = 0x00,    // Universal Host Controller Interface (USB 1.1, Intel)
    EHCI = 0x20,    // Enhanced Host Controller Interface (USB 2.0)
    XHCI = 0x30     // Extensible Host Controller Interface (USB 3.x)
}

/// <summary>
/// USB device speed classes.
/// </summary>
public enum UsbDeviceSpeed
{
    Unknown = 0,
    LowSpeed = 1,       // USB 1.0 - 1.5 Mbps
    FullSpeed = 2,      // USB 1.1 - 12 Mbps
    HighSpeed = 3,      // USB 2.0 - 480 Mbps
    SuperSpeed = 4,     // USB 3.0 - 5 Gbps
    SuperSpeedPlus = 5  // USB 3.1/3.2 - 10/20 Gbps
}

/// <summary>
/// USB physical port connector types for macOS.
/// </summary>
public enum UsbConnectorType
{
    [JsonPropertyName("USB 2 Type A")]
    USB2TypeA = 0,

    [JsonPropertyName("USB Mini-AB")]
    MiniAB = 1,

    [JsonPropertyName("ExpressCard")]
    ExpressCard = 2,

    [JsonPropertyName("USB 3 Type A")]
    USB3TypeA = 3,

    [JsonPropertyName("USB 3 Type B")]
    USB3TypeB = 4,

    [JsonPropertyName("USB 3 Micro-B")]
    USB3MicroB = 5,

    [JsonPropertyName("USB 3 Micro-AB")]
    USB3MicroAB = 6,

    [JsonPropertyName("USB 3 Power-B")]
    USB3PowerB = 7,

    [JsonPropertyName("Type C - USB 2 only")]
    TypeCUsb2Only = 8,

    [JsonPropertyName("Type C - with switch")]
    TypeCWithSwitch = 9,

    [JsonPropertyName("Type C - without switch")]
    TypeCWithoutSwitch = 10,

    [JsonPropertyName("Internal")]
    Internal = 255
}

/// <summary>
/// Represents a USB controller (XHCI, EHCI, etc.).
/// </summary>
public sealed class UsbController
{
    public string Name { get; set; } = "";
    public UsbControllerType ControllerType { get; set; } = UsbControllerType.Unknown;
    public string? HubName { get; set; }
    public List<UsbPort> Ports { get; set; } = [];
    public UsbControllerIdentifiers Identifiers { get; set; } = new();
    public int SelectedCount { get; set; }
}

/// <summary>
/// Controller identifiers for matching in kext.
/// </summary>
public sealed class UsbControllerIdentifiers
{
    public string? InstanceId { get; set; }
    public string? AcpiPath { get; set; }
    public string? DriverKey { get; set; }
    public List<string>? LocationPaths { get; set; }
    public string? Path { get; set; }
    public string[]? PciId { get; set; }  // [vendor, device, subvendor?, subdevice?]
    public int[]? Bdf { get; set; }       // Bus, Device, Function
    public int? LocationId { get; set; }
    public int? BusNumber { get; set; }
    public int? PciRevision { get; set; }
    public string? IoName { get; set; }   // macOS IORegistry name
}

/// <summary>
/// Represents a USB port on a controller.
/// </summary>
public sealed class UsbPort
{
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public UsbDeviceSpeed SpeedClass { get; set; } = UsbDeviceSpeed.Unknown;
    public UsbConnectorType? ConnectorType { get; set; }
    public UsbConnectorType? GuessedType { get; set; }
    public List<UsbDevice> Devices { get; set; } = [];
    public bool Selected { get; set; }
    public int SelectionIndex { get; set; }
    public string? Comment { get; set; }
    public UsbCompanionInfo? CompanionInfo { get; set; }
}

/// <summary>
/// Companion port information (USB2/USB3 binding).
/// </summary>
public sealed class UsbCompanionInfo
{
    public string? Hub { get; set; }
    public int? Port { get; set; }
}

/// <summary>
/// Represents a connected USB device.
/// </summary>
public sealed class UsbDevice
{
    public string Name { get; set; } = "";
    public string? InstanceId { get; set; }
    public UsbDeviceSpeed Speed { get; set; } = UsbDeviceSpeed.Unknown;
    public List<UsbDevice> Devices { get; set; } = [];  // Child devices (hubs)
    public string? Error { get; set; }
}

/// <summary>
/// USB mapping settings.
/// </summary>
public sealed class UsbMapperSettings
{
    public bool ShowFriendlyTypes { get; set; } = true;
    public bool UseNativeClasses { get; set; } = false;
    public bool UseLegacyNativeClasses { get; set; } = false;
    public bool AddCommentsToMap { get; set; } = true;
    public bool AutoBindCompanions { get; set; } = true;
}

/// <summary>
/// USB mapping session data.
/// </summary>
public sealed class UsbMappingData
{
    public List<UsbController> Controllers { get; set; } = [];
    public UsbMapperSettings Settings { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
