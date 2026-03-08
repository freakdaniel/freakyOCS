using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.UsbMapper;

public sealed partial class UsbMapperService
{
    // macOS-specific controller enumeration using ioreg

    private async Task<List<UsbController>> GetMacOSControllersAsync(CancellationToken ct)
    {
        var controllers = new List<UsbController>();

        try
        {
            // Get USB controllers from IORegistry
            var ioregOutput = await RunIoregAsync("-lx -c IOUSBHostDevice", ct);
            if (!string.IsNullOrEmpty(ioregOutput))
            {
                controllers = ParseIoregOutput(ioregOutput);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enumerate USB controllers on macOS");
        }

        return controllers;
    }

    private async Task<string?> RunIoregAsync(string arguments, CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/sbin/ioreg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("ioreg failed: {Error}", ex.Message);
            return null;
        }
    }

    private List<UsbController> ParseIoregOutput(string output)
    {
        var controllers = new List<UsbController>();
        UsbController? currentController = null;
        var portStack = new Stack<(int indent, UsbPort? port, UsbDevice? device)>();

        var lines = output.Split('\n');
        var indentRegex = new Regex(@"^(\s*)\+-o\s+(.+)\s+<");
        var propertyRegex = new Regex(@"^\s*[|]\s*""([^""]+)""\s*=\s*(.+)$");

        var currentProperties = new Dictionary<string, string>();
        int currentIndent = 0;
        string? currentName = null;

        foreach (var line in lines)
        {
            // Check for new device entry
            var objectMatch = indentRegex.Match(line);
            if (objectMatch.Success)
            {
                // Process previous entry
                if (currentName is not null)
                {
                    ProcessMacOsEntry(currentName, currentIndent, currentProperties,
                        ref currentController, controllers, portStack);
                }

                currentIndent = objectMatch.Groups[1].Value.Length;
                currentName = objectMatch.Groups[2].Value;
                currentProperties.Clear();
                continue;
            }

            // Parse property
            var propMatch = propertyRegex.Match(line);
            if (propMatch.Success)
            {
                currentProperties[propMatch.Groups[1].Value] = propMatch.Groups[2].Value;
            }
        }

        // Process last entry
        if (currentName is not null)
        {
            ProcessMacOsEntry(currentName, currentIndent, currentProperties,
                ref currentController, controllers, portStack);
        }

        return controllers;
    }

    private void ProcessMacOsEntry(string name, int indent, Dictionary<string, string> props,
        ref UsbController? controller, List<UsbController> controllers,
        Stack<(int indent, UsbPort? port, UsbDevice? device)> stack)
    {
        // Check if this is a controller
        if (IsUsbController(name, props))
        {
            controller = CreateMacOsController(name, props);
            controllers.Add(controller);
            stack.Clear();
            return;
        }

        if (controller is null)
            return;

        // Pop items that are deeper than current
        while (stack.Count > 0 && stack.Peek().indent >= indent)
        {
            stack.Pop();
        }

        // Check if this is a port or device
        if (props.TryGetValue("port", out var portValue) || name.Contains("Port"))
        {
            var port = CreateMacOsPort(name, props, controller.Ports.Count + 1);
            controller.Ports.Add(port);
            stack.Push((indent, port, null));
        }
        else if (props.TryGetValue("USB Product Name", out _) ||
                 props.TryGetValue("USB Vendor Name", out _))
        {
            var device = CreateMacOsDevice(name, props);

            // Find parent port in stack
            foreach (var item in stack)
            {
                if (item.port is not null)
                {
                    item.port.Devices.Add(device);
                    break;
                }
                if (item.device is not null)
                {
                    item.device.Devices.Add(device);
                    break;
                }
            }

            stack.Push((indent, null, device));
        }
    }

    private static bool IsUsbController(string name, Dictionary<string, string> props)
    {
        var upper = name.ToUpperInvariant();
        return upper.Contains("XHC") ||
               upper.Contains("EHC") ||
               upper.Contains("OHC") ||
               upper.Contains("UHC") ||
               upper.Contains("USB HOST") ||
               props.ContainsKey("IOPCIClassMatch") ||
               (props.TryGetValue("IOClass", out var ioClass) &&
                ioClass.Contains("USB"));
    }

    private UsbController CreateMacOsController(string name, Dictionary<string, string> props)
    {
        var controller = new UsbController
        {
            Name = name,
            ControllerType = DetermineMacOsControllerType(name, props),
            Identifiers = new UsbControllerIdentifiers()
        };

        // Parse identifiers
        if (props.TryGetValue("IORegistryEntryName", out var regName))
        {
            controller.Identifiers.AcpiPath = regName;
        }

        if (props.TryGetValue("vendor-id", out var vendorId))
        {
            controller.Identifiers.PciId ??= new string[4];
            controller.Identifiers.PciId[0] = ParseHexValue(vendorId)!;
        }

        if (props.TryGetValue("device-id", out var deviceId))
        {
            controller.Identifiers.PciId ??= new string[4];
            controller.Identifiers.PciId[1] = ParseHexValue(deviceId)!;
        }

        if (props.TryGetValue("acpi-path", out var acpiPath))
        {
            controller.Identifiers.AcpiPath = acpiPath.Trim('"');
        }

        if (props.TryGetValue("name", out var ioName))
        {
            controller.Identifiers.IoName = ioName.Trim('"');
        }

        return controller;
    }

    private static UsbControllerType DetermineMacOsControllerType(string name, Dictionary<string, string> props)
    {
        var text = name.ToUpperInvariant();

        if (props.TryGetValue("IOClass", out var ioClass))
            text += " " + ioClass.ToUpperInvariant();

        if (text.Contains("XHC") || text.Contains("USB3"))
            return UsbControllerType.XHCI;
        if (text.Contains("EHC") || text.Contains("USB2") || text.Contains("ENHANCED"))
            return UsbControllerType.EHCI;
        if (text.Contains("OHC"))
            return UsbControllerType.OHCI;
        if (text.Contains("UHC"))
            return UsbControllerType.UHCI;

        return UsbControllerType.Unknown;
    }

    private static string? ParseHexValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // Handle <XXXX> format
        var match = Regex.Match(value, @"<([0-9a-fA-F]+)>");
        if (match.Success)
        {
            var hex = match.Groups[1].Value;
            // Reverse bytes (little endian)
            if (hex.Length >= 4)
            {
                return string.Concat(hex.Chunk(2).Reverse().SelectMany(c => c)).Substring(0, 4).ToUpperInvariant();
            }
            return hex.ToUpperInvariant();
        }

        return value.Trim('"').ToUpperInvariant();
    }

    private static UsbPort CreateMacOsPort(string name, Dictionary<string, string> props, int index)
    {
        var port = new UsbPort
        {
            Index = index,
            Name = name.Replace("@", "").Trim()
        };

        // Parse port number
        if (props.TryGetValue("port", out var portNum))
        {
            var hexMatch = Regex.Match(portNum, @"<([0-9a-fA-F]+)>");
            if (hexMatch.Success && int.TryParse(hexMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out var num))
            {
                port.Index = num;
            }
        }

        // Parse connector type
        if (props.TryGetValue("UsbConnector", out var connector))
        {
            var hexMatch = Regex.Match(connector, @"(\d+)");
            if (hexMatch.Success && int.TryParse(hexMatch.Groups[1].Value, out var connType))
            {
                port.ConnectorType = (UsbConnectorType)connType;
            }
        }

        // Guess port speed from name
        var upper = port.Name.ToUpperInvariant();
        if (upper.StartsWith("SS"))
        {
            port.SpeedClass = UsbDeviceSpeed.SuperSpeed;
            port.GuessedType ??= UsbConnectorType.USB3TypeA;
        }
        else if (upper.StartsWith("HS"))
        {
            port.SpeedClass = UsbDeviceSpeed.HighSpeed;
            port.GuessedType ??= UsbConnectorType.USB2TypeA;
        }

        return port;
    }

    private static UsbDevice CreateMacOsDevice(string name, Dictionary<string, string> props)
    {
        var productName = props.TryGetValue("USB Product Name", out var p)
            ? p.Trim('"')
            : name;

        var vendorName = props.TryGetValue("USB Vendor Name", out var v)
            ? v.Trim('"')
            : null;

        var device = new UsbDevice
        {
            Name = vendorName is not null ? $"{vendorName} {productName}" : productName
        };

        // Parse speed
        if (props.TryGetValue("Device Speed", out var speed))
        {
            device.Speed = speed switch
            {
                var s when s.Contains("5") => UsbDeviceSpeed.SuperSpeed,
                var s when s.Contains("480") => UsbDeviceSpeed.HighSpeed,
                var s when s.Contains("12") => UsbDeviceSpeed.FullSpeed,
                _ => UsbDeviceSpeed.Unknown
            };
        }

        if (props.TryGetValue("IORegistryEntryName", out var regName))
        {
            device.InstanceId = regName;
        }

        return device;
    }
}
