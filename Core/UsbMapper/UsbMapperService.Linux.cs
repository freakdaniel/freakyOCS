using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.UsbMapper;

public sealed partial class UsbMapperService
{
    // Linux-specific controller enumeration using /sys/bus/usb and lspci

    private async Task<List<UsbController>> GetLinuxControllersAsync(CancellationToken ct)
    {
        var controllers = new List<UsbController>();

        try
        {
            // Get PCI USB controllers
            var pciControllers = await GetPciUsbControllersAsync(ct);
            controllers.AddRange(pciControllers);

            // Enumerate USB devices from sysfs
            await EnumerateLinuxUsbDevicesAsync(controllers, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enumerate USB controllers on Linux");
        }

        return controllers;
    }

    private async Task<List<UsbController>> GetPciUsbControllersAsync(CancellationToken ct)
    {
        var controllers = new List<UsbController>();

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "lspci",
                Arguments = "-nnD",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // Parse lspci output for USB controllers
            // Format: 0000:00:14.0 USB controller [0c03]: Intel USB 3.0 xHCI Host Controller [8086:a36d]
            var regex = new Regex(@"^([0-9a-f:.]+)\s+USB controller\s+\[0c03\]:\s+(.+?)\s+\[([0-9a-f]{4}):([0-9a-f]{4})\]",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Match match in regex.Matches(output))
            {
                var bdf = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                var vendorId = match.Groups[3].Value;
                var deviceId = match.Groups[4].Value;

                var controllerType = DetermineControllerType(name);

                var controller = new UsbController
                {
                    Name = name,
                    ControllerType = controllerType,
                    Identifiers = new UsbControllerIdentifiers
                    {
                        PciId = [vendorId.ToUpperInvariant(), deviceId.ToUpperInvariant()],
                        Bdf = ParseBdf(bdf)
                    }
                };

                // Find USB host controller directory
                var sysPath = $"/sys/bus/pci/devices/{bdf}";
                if (Directory.Exists(sysPath))
                {
                    controller.Identifiers.Path = sysPath;

                    // Find USB root hub
                    var usbDir = Directory.GetDirectories(sysPath, "usb*").FirstOrDefault();
                    if (usbDir is not null)
                    {
                        controller.HubName = Path.GetFileName(usbDir);
                    }
                }

                controllers.Add(controller);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("lspci failed: {Error}", ex.Message);
        }

        return controllers;
    }

    private static int[]? ParseBdf(string bdf)
    {
        // Parse "0000:00:14.0" format
        var match = Regex.Match(bdf, @"([0-9a-f]{4}):([0-9a-f]{2}):([0-9a-f]{2})\.([0-9a-f]+)", RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        return
        [
            Convert.ToInt32(match.Groups[1].Value, 16),
            Convert.ToInt32(match.Groups[2].Value, 16),
            Convert.ToInt32(match.Groups[3].Value, 16),
            Convert.ToInt32(match.Groups[4].Value, 16)
        ];
    }

    private static UsbControllerType DetermineControllerType(string name)
    {
        var upper = name.ToUpperInvariant();

        if (upper.Contains("XHCI") || upper.Contains("USB3") || upper.Contains("USB 3"))
            return UsbControllerType.XHCI;
        if (upper.Contains("EHCI") || upper.Contains("USB2") || upper.Contains("USB 2") || upper.Contains("ENHANCED"))
            return UsbControllerType.EHCI;
        if (upper.Contains("OHCI"))
            return UsbControllerType.OHCI;
        if (upper.Contains("UHCI"))
            return UsbControllerType.UHCI;

        return UsbControllerType.Unknown;
    }

    private async Task EnumerateLinuxUsbDevicesAsync(List<UsbController> controllers, CancellationToken ct)
    {
        const string sysUsbPath = "/sys/bus/usb/devices";

        if (!Directory.Exists(sysUsbPath))
            return;

        await Task.Run(() =>
        {
            try
            {
                // Find root hubs (directories like usb1, usb2, etc.)
                var rootHubs = Directory.GetDirectories(sysUsbPath)
                    .Select(Path.GetFileName)
                    .Where(n => n!.StartsWith("usb"))
                    .OrderBy(n => n)
                    .ToArray();

                foreach (var hubName in rootHubs)
                {
                    var hubPath = Path.Combine(sysUsbPath, hubName!);
                    var controller = controllers.FirstOrDefault(c => c.HubName == hubName);

                    if (controller is null)
                    {
                        // Create controller from hub info
                        controller = new UsbController
                        {
                            Name = ReadSysfsAttribute(hubPath, "product") ?? $"USB Controller {hubName}",
                            HubName = hubName
                        };

                        var speedStr = ReadSysfsAttribute(hubPath, "speed");
                        if (int.TryParse(speedStr, out var speed))
                        {
                            controller.ControllerType = speed switch
                            {
                                >= 5000 => UsbControllerType.XHCI,
                                >= 480 => UsbControllerType.EHCI,
                                >= 12 => UsbControllerType.OHCI,
                                _ => UsbControllerType.Unknown
                            };
                        }

                        controllers.Add(controller);
                    }

                    // Enumerate ports
                    EnumerateLinuxPorts(hubPath, hubName!, controller);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Failed to enumerate Linux USB devices: {Error}", ex.Message);
            }
        }, ct);
    }

    private void EnumerateLinuxPorts(string hubPath, string hubName, UsbController controller)
    {
        try
        {
            // Get max number of ports
            var maxChildStr = ReadSysfsAttribute(hubPath, "maxchild");
            var maxPorts = int.TryParse(maxChildStr, out var mp) ? mp : 16;

            for (int i = 1; i <= maxPorts; i++)
            {
                var portPath = Path.Combine(hubPath, $"{hubName.Replace("usb", "")}-{i}");
                var portExists = Directory.Exists(portPath);

                var port = new UsbPort
                {
                    Index = i,
                    Name = GeneratePortName(controller.ControllerType, i)
                };

                if (portExists)
                {
                    // Read port properties
                    var speedStr = ReadSysfsAttribute(portPath, "speed");
                    if (int.TryParse(speedStr, out var speed))
                    {
                        port.SpeedClass = speed switch
                        {
                            >= 10000 => UsbDeviceSpeed.SuperSpeedPlus,
                            >= 5000 => UsbDeviceSpeed.SuperSpeed,
                            >= 480 => UsbDeviceSpeed.HighSpeed,
                            >= 12 => UsbDeviceSpeed.FullSpeed,
                            _ => UsbDeviceSpeed.LowSpeed
                        };
                    }

                    // Read device info
                    var device = ReadLinuxDevice(portPath);
                    if (device is not null)
                    {
                        port.Devices.Add(device);
                    }

                    // Guess connector type
                    port.GuessedType = port.SpeedClass switch
                    {
                        UsbDeviceSpeed.SuperSpeed or UsbDeviceSpeed.SuperSpeedPlus => UsbConnectorType.USB3TypeA,
                        _ => UsbConnectorType.USB2TypeA
                    };
                }

                controller.Ports.Add(port);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to enumerate ports for {Hub}: {Error}", hubName, ex.Message);
        }
    }

    private static string GeneratePortName(UsbControllerType type, int index)
    {
        var prefix = type switch
        {
            UsbControllerType.XHCI => index <= 10 ? "HS" : "SS",
            UsbControllerType.EHCI => "HS",
            _ => "PRT"
        };

        var portNum = type == UsbControllerType.XHCI && index > 10 ? index - 10 : index;
        return $"{prefix}{portNum:D2}";
    }

    private UsbDevice? ReadLinuxDevice(string devicePath)
    {
        try
        {
            var product = ReadSysfsAttribute(devicePath, "product");
            var manufacturer = ReadSysfsAttribute(devicePath, "manufacturer");

            if (product is null && manufacturer is null)
                return null;

            var name = string.Join(" ", new[] { manufacturer, product }.Where(s => !string.IsNullOrEmpty(s)));

            var device = new UsbDevice
            {
                Name = name,
                InstanceId = Path.GetFileName(devicePath)
            };

            var speedStr = ReadSysfsAttribute(devicePath, "speed");
            if (int.TryParse(speedStr, out var speed))
            {
                device.Speed = speed switch
                {
                    >= 10000 => UsbDeviceSpeed.SuperSpeedPlus,
                    >= 5000 => UsbDeviceSpeed.SuperSpeed,
                    >= 480 => UsbDeviceSpeed.HighSpeed,
                    >= 12 => UsbDeviceSpeed.FullSpeed,
                    _ => UsbDeviceSpeed.LowSpeed
                };
            }

            // Check for child devices (hubs)
            var childDirs = Directory.GetDirectories(devicePath)
                .Where(d => Regex.IsMatch(Path.GetFileName(d), @"^\d+-\d+"));

            foreach (var childPath in childDirs)
            {
                var childDevice = ReadLinuxDevice(childPath);
                if (childDevice is not null)
                {
                    device.Devices.Add(childDevice);
                }
            }

            return device;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadSysfsAttribute(string path, string attribute)
    {
        var filePath = Path.Combine(path, attribute);
        if (!File.Exists(filePath))
            return null;

        try
        {
            return File.ReadAllText(filePath).Trim();
        }
        catch
        {
            return null;
        }
    }
}
