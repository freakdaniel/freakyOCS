#pragma warning disable CA1416

using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.UsbMapper;

public sealed partial class UsbMapperService
{
    // Windows-specific controller enumeration using WMI and SetupAPI

    private async Task<List<UsbController>> GetWindowsControllersAsync(CancellationToken ct)
    {
        var controllers = new List<UsbController>();

        try
        {
            // First try to use usbdump if available, otherwise use WMI
            var usbDumpResult = await TryGetUsbDumpControllersAsync(ct);
            if (usbDumpResult is not null)
            {
                controllers = usbDumpResult;
            }
            else
            {
                controllers = await GetWmiControllersAsync(ct);
            }

            // Enrich with WMI properties
            foreach (var controller in controllers)
            {
                await EnrichControllerWithWmiAsync(controller, ct);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enumerate USB controllers on Windows");
        }

        return controllers;
    }

    private async Task<List<UsbController>?> TryGetUsbDumpControllersAsync(CancellationToken ct)
    {
        // Try to find usbdump.exe
        var exePath = Path.Combine(AppContext.BaseDirectory, "usbdump.exe");
        if (!File.Exists(exePath))
            return null;

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "-j",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
                return null;

            return ParseUsbDumpOutput(output);
        }
        catch
        {
            return null;
        }
    }

    private List<UsbController>? ParseUsbDumpOutput(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var controllers = new List<UsbController>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var controller = new UsbController
                {
                    Name = element.TryGetProperty("name", out var name) ? name.GetString() ?? "Unknown" : "Unknown",
                    HubName = element.TryGetProperty("hub_name", out var hub) ? hub.GetString() : null
                };

                // Parse identifiers
                if (element.TryGetProperty("identifiers", out var ids))
                {
                    controller.Identifiers = new UsbControllerIdentifiers
                    {
                        InstanceId = ids.TryGetProperty("instance_id", out var iid) ? iid.GetString() : null,
                        Path = ids.TryGetProperty("path", out var path) ? path.GetString() : null
                    };

                    if (ids.TryGetProperty("pci_id", out var pciId))
                    {
                        controller.Identifiers.PciId = pciId.EnumerateArray()
                            .Select(x => x.GetString() ?? "")
                            .ToArray();
                    }

                    if (ids.TryGetProperty("bdf", out var bdf))
                    {
                        controller.Identifiers.Bdf = bdf.EnumerateArray()
                            .Select(x => x.GetInt32())
                            .ToArray();
                    }
                }

                // Parse ports
                if (element.TryGetProperty("ports", out var ports))
                {
                    foreach (var portElement in ports.EnumerateArray())
                    {
                        var port = new UsbPort
                        {
                            Name = portElement.TryGetProperty("name", out var pn) ? pn.GetString() ?? "Port" : "Port",
                            Index = portElement.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0,
                            SpeedClass = portElement.TryGetProperty("class", out var cls)
                                ? (UsbDeviceSpeed)cls.GetInt32()
                                : UsbDeviceSpeed.Unknown
                        };

                        if (portElement.TryGetProperty("guessed", out var guessed) && !guessed.ValueKind.Equals(JsonValueKind.Null))
                        {
                            port.GuessedType = (UsbConnectorType)guessed.GetInt32();
                        }

                        if (portElement.TryGetProperty("companion_info", out var companion))
                        {
                            port.CompanionInfo = new UsbCompanionInfo
                            {
                                Hub = companion.TryGetProperty("hub", out var ch) ? ch.GetString() : null,
                                Port = companion.TryGetProperty("port", out var cp) ? cp.GetInt32() : null
                            };
                        }

                        // Parse devices
                        if (portElement.TryGetProperty("devices", out var devices))
                        {
                            port.Devices = ParseDevices(devices);
                        }

                        controller.Ports.Add(port);
                    }
                }

                controllers.Add(controller);
            }

            return controllers;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to parse usbdump output: {Error}", ex.Message);
            return null;
        }
    }

    private List<UsbDevice> ParseDevices(JsonElement element)
    {
        var devices = new List<UsbDevice>();

        foreach (var deviceElement in element.EnumerateArray())
        {
            if (deviceElement.ValueKind == JsonValueKind.String)
            {
                devices.Add(new UsbDevice { Name = deviceElement.GetString() ?? "Unknown" });
                continue;
            }

            if (deviceElement.ValueKind != JsonValueKind.Object)
                continue;

            var device = new UsbDevice
            {
                Name = deviceElement.TryGetProperty("name", out var name) ? name.GetString() ?? "Unknown" : "Unknown",
                InstanceId = deviceElement.TryGetProperty("instance_id", out var iid) ? iid.GetString() : null,
                Speed = deviceElement.TryGetProperty("speed", out var speed)
                    ? (UsbDeviceSpeed)speed.GetInt32()
                    : UsbDeviceSpeed.Unknown
            };

            if (deviceElement.TryGetProperty("error", out var error))
            {
                device.Error = error.GetString();
            }

            if (deviceElement.TryGetProperty("devices", out var children))
            {
                device.Devices = ParseDevices(children);
            }

            devices.Add(device);
        }

        return devices;
    }

    private async Task<List<UsbController>> GetWmiControllersAsync(CancellationToken ct)
    {
        var controllers = new List<UsbController>();

        await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_USBController");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var controller = new UsbController
                    {
                        Name = obj["Name"]?.ToString() ?? "USB Controller",
                        Identifiers = new UsbControllerIdentifiers
                        {
                            InstanceId = obj["PNPDeviceID"]?.ToString()
                        }
                    };

                    // Determine controller type from name/service
                    var name = controller.Name.ToUpperInvariant();
                    if (name.Contains("XHCI") || name.Contains("USB 3"))
                        controller.ControllerType = UsbControllerType.XHCI;
                    else if (name.Contains("EHCI") || name.Contains("USB 2") || name.Contains("ENHANCED"))
                        controller.ControllerType = UsbControllerType.EHCI;
                    else if (name.Contains("OHCI"))
                        controller.ControllerType = UsbControllerType.OHCI;
                    else if (name.Contains("UHCI"))
                        controller.ControllerType = UsbControllerType.UHCI;

                    // Get hub information
                    controller.Ports = GetWmiPortsForController(controller.Identifiers.InstanceId);

                    controllers.Add(controller);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("WMI query failed: {Error}", ex.Message);
            }
        }, ct);

        return controllers;
    }

    private List<UsbPort> GetWmiPortsForController(string? instanceId)
    {
        var ports = new List<UsbPort>();
        if (string.IsNullOrEmpty(instanceId)) return ports;

        try
        {
            // Query USB hubs
            using var hubSearcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_USBHub");

            int portIndex = 1;
            foreach (ManagementObject hub in hubSearcher.Get())
            {
                var hubInstanceId = hub["PNPDeviceID"]?.ToString() ?? "";

                // Get connected devices
                var devices = GetUsbDevicesForHub(hubInstanceId);

                // Create port entries
                foreach (var device in devices)
                {
                    var port = new UsbPort
                    {
                        Name = $"Port {portIndex}",
                        Index = portIndex,
                        SpeedClass = GuessDeviceSpeed(device),
                        Devices = [device]
                    };

                    // Guess port type
                    port.GuessedType = port.SpeedClass switch
                    {
                        UsbDeviceSpeed.SuperSpeed or UsbDeviceSpeed.SuperSpeedPlus => UsbConnectorType.USB3TypeA,
                        _ => UsbConnectorType.USB2TypeA
                    };

                    ports.Add(port);
                    portIndex++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to get USB ports: {Error}", ex.Message);
        }

        return ports;
    }

    private List<UsbDevice> GetUsbDevicesForHub(string hubInstanceId)
    {
        var devices = new List<UsbDevice>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_USBHub.DeviceID='{hubInstanceId.Replace("\\", "\\\\")}'}} " +
                "WHERE AssocClass = Win32_USBControllerDevice");

            foreach (ManagementObject obj in searcher.Get())
            {
                var device = new UsbDevice
                {
                    Name = obj["Name"]?.ToString() ?? obj["Description"]?.ToString() ?? "USB Device",
                    InstanceId = obj["PNPDeviceID"]?.ToString()
                };

                devices.Add(device);
            }
        }
        catch
        {
            // Ignore errors for individual devices
        }

        return devices;
    }

    private static UsbDeviceSpeed GuessDeviceSpeed(UsbDevice device)
    {
        var name = device.Name.ToUpperInvariant();

        if (name.Contains("USB 3.2") || name.Contains("SUPERSPEED+"))
            return UsbDeviceSpeed.SuperSpeedPlus;
        if (name.Contains("USB 3") || name.Contains("SUPERSPEED"))
            return UsbDeviceSpeed.SuperSpeed;
        if (name.Contains("USB 2") || name.Contains("HIGH SPEED"))
            return UsbDeviceSpeed.HighSpeed;

        return UsbDeviceSpeed.HighSpeed; // Default assumption
    }

    private async Task EnrichControllerWithWmiAsync(UsbController controller, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(controller.Identifiers.InstanceId))
            return;

        await Task.Run(() =>
        {
            try
            {
                var instanceId = controller.Identifiers.InstanceId.Replace("\\", "\\\\");

                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID = '{instanceId}'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    // Try to get additional properties
                    try
                    {
                        var props = obj.InvokeMethod("GetDeviceProperties",
                            new object[] { new[] { "DEVPKEY_Device_BiosDeviceName", "DEVPKEY_Device_LocationPaths" } })
                            as ManagementBaseObject[];

                        // Parse ACPI path and location info if available
                    }
                    catch
                    {
                        // Properties not available
                    }

                    // Parse PCI ID from instance ID
                    var match = Regex.Match(controller.Identifiers.InstanceId,
                        @"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})(?:&SUBSYS_([0-9A-F]{4})([0-9A-F]{4}))?",
                        RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        controller.Identifiers.PciId = match.Groups[3].Success
                            ? [match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value]
                            : [match.Groups[1].Value, match.Groups[2].Value];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Failed to enrich controller {Name}: {Error}", controller.Name, ex.Message);
            }
        }, ct);
    }

    /// <summary>
    /// Merges new controller data into historical data.
    /// </summary>
    private static void MergeControllers(List<UsbController> historical, List<UsbController> current)
    {
        foreach (var controller in current)
        {
            var existing = historical.FirstOrDefault(c => IsSameController(c, controller));

            if (existing is null)
            {
                historical.Add(controller);
                continue;
            }

            // Merge ports
            foreach (var port in controller.Ports)
            {
                var existingPort = existing.Ports.FirstOrDefault(p => p.Index == port.Index);

                if (existingPort is null)
                {
                    existing.Ports.Add(port);
                }
                else
                {
                    // Merge devices
                    foreach (var device in port.Devices)
                    {
                        if (!existingPort.Devices.Any(d => d.Name == device.Name))
                        {
                            existingPort.Devices.Add(device);
                        }
                    }
                }
            }

            existing.Ports = [.. existing.Ports.OrderBy(p => p.Index)];
        }
    }

    private static bool IsSameController(UsbController a, UsbController b)
    {
        var idA = a.Identifiers;
        var idB = b.Identifiers;

        // Check by PCI ID
        if (idA.PciId is not null && idB.PciId is not null &&
            idA.PciId.Length >= 2 && idB.PciId.Length >= 2)
        {
            if (idA.PciId[0] == idB.PciId[0] && idA.PciId[1] == idB.PciId[1])
                return true;
        }

        // Check by BDF
        if (idA.Bdf is not null && idB.Bdf is not null &&
            idA.Bdf.SequenceEqual(idB.Bdf))
            return true;

        // Check by ACPI path
        if (!string.IsNullOrEmpty(idA.AcpiPath) && idA.AcpiPath == idB.AcpiPath)
            return true;

        // Check by name as fallback
        return a.Name == b.Name;
    }
}
