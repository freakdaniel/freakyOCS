using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Services;

namespace OcsNet.Core.HardwareSniffer;

public sealed partial class HardwareSnifferService
{
    private async Task CollectMacOSHardwareAsync(HardwareReport report, CancellationToken ct)
    {
        // Get system profiler data
        var spOutput = await RunCommandAsync("system_profiler", 
            "SPHardwareDataType SPDisplaysDataType SPAudioDataType SPNetworkDataType SPNVMeDataType SPSerialATADataType SPBluetoothDataType -json", ct);

        if (string.IsNullOrEmpty(spOutput))
            return;

        try
        {
            using var doc = JsonDocument.Parse(spOutput);
            var root = doc.RootElement;

            // CPU info
            if (root.TryGetProperty("SPHardwareDataType", out var hwArray) && hwArray.GetArrayLength() > 0)
            {
                var hw = hwArray[0];
                report.Cpu = new CpuInfo
                {
                    ProcessorName = hw.TryGetProperty("cpu_type", out var cpu) ? cpu.GetString() ?? "" : "",
                    CoreCount = hw.TryGetProperty("number_processors", out var cores) ? 
                        int.TryParse(cores.GetString()?.Split(' ')[0], out var c) ? c : 0 : 0,
                    Manufacturer = hw.TryGetProperty("cpu_type", out var cpuType) && 
                        cpuType.GetString()?.Contains("Intel") == true ? "GenuineIntel" : "Apple"
                };

                // Detect desktop vs laptop
                var modelName = hw.TryGetProperty("machine_model", out var model) ? model.GetString() ?? "" : "";
                report.Motherboard = new MotherboardInfo
                {
                    Name = modelName,
                    Platform = modelName.Contains("Book") ? "Laptop" : "Desktop"
                };
            }

            // GPU info
            if (root.TryGetProperty("SPDisplaysDataType", out var gpuArray))
            {
                report.Gpus = new Dictionary<string, GpuInfo>();
                foreach (var gpu in gpuArray.EnumerateArray())
                {
                    var name = gpu.TryGetProperty("sppci_model", out var model) ? model.GetString() ?? "Unknown" : "Unknown";
                    var vendor = gpu.TryGetProperty("sppci_vendor", out var v) ? v.GetString() ?? "" : "";
                    var deviceId = gpu.TryGetProperty("sppci_device_id", out var did) ? did.GetString()?.Replace("0x", "") ?? "" : "";
                    var vendorId = gpu.TryGetProperty("sppci_vendor_id", out var vid) ? vid.GetString()?.Replace("0x", "") ?? "" : "";

                    var manufacturer = vendor.Contains("Intel") ? "Intel" :
                                       vendor.Contains("AMD") || vendor.Contains("ATI") ? "AMD" :
                                       vendor.Contains("NVIDIA") ? "NVIDIA" : "Apple";

                    report.Gpus[name] = new GpuInfo
                    {
                        DeviceId = $"{vendorId}-{deviceId}".ToUpperInvariant(),
                        Manufacturer = manufacturer,
                        DeviceType = gpu.TryGetProperty("sppci_bus", out var bus) && 
                            bus.GetString()?.Contains("Built") == true ? "Integrated GPU" : "Discrete GPU"
                    };
                }
            }

            // Audio info
            if (root.TryGetProperty("SPAudioDataType", out var audioArray))
            {
                report.Sound = new Dictionary<string, AudioInfo>();
                foreach (var audio in audioArray.EnumerateArray())
                {
                    var name = audio.TryGetProperty("_name", out var n) ? n.GetString() ?? "Audio" : "Audio";
                    var deviceId = audio.TryGetProperty("coreaudio_device_transport", out var t) ? 
                        t.GetString() ?? "" : "";

                    report.Sound[name] = new AudioInfo
                    {
                        DeviceId = deviceId,
                        BusType = "PCI"
                    };
                }
            }

            // Network info (needs ioreg for PCI IDs)
            report.Network = await CollectMacOSNetworkAsync(ct);

            // Storage info
            report.StorageControllers = new Dictionary<string, StorageInfo>();
            
            if (root.TryGetProperty("SPNVMeDataType", out var nvmeArray))
            {
                foreach (var nvme in nvmeArray.EnumerateArray())
                {
                    var name = nvme.TryGetProperty("_name", out var n) ? n.GetString() ?? "NVMe" : "NVMe";
                    var deviceId = nvme.TryGetProperty("device_id", out var did) ? did.GetString() ?? "" : "";
                    var vendorId = nvme.TryGetProperty("vendor_id", out var vid) ? vid.GetString() ?? "" : "";

                    report.StorageControllers[name] = new StorageInfo
                    {
                        DeviceId = $"{vendorId}-{deviceId}".Replace("0x", "").ToUpperInvariant(),
                        BusType = "NVMe",
                        SubsystemId = ""
                    };
                }
            }

            if (root.TryGetProperty("SPSerialATADataType", out var sataArray))
            {
                foreach (var sata in sataArray.EnumerateArray())
                {
                    var name = sata.TryGetProperty("_name", out var n) ? n.GetString() ?? "SATA" : "SATA";
                    report.StorageControllers[name] = new StorageInfo
                    {
                        DeviceId = "",
                        BusType = "SATA",
                        SubsystemId = ""
                    };
                }
            }

            // Bluetooth
            if (root.TryGetProperty("SPBluetoothDataType", out var btArray) && btArray.GetArrayLength() > 0)
            {
                report.Bluetooth = new Dictionary<string, BluetoothInfo>();
                var bt = btArray[0];
                
                if (bt.TryGetProperty("local_device_title", out var localDevice))
                {
                    var name = localDevice.TryGetProperty("general_device_name", out var dn) ? 
                        dn.GetString() ?? "Bluetooth" : "Bluetooth";
                    var vendorId = localDevice.TryGetProperty("general_vendor_id", out var vid) ? 
                        vid.GetString() ?? "" : "";
                    var productId = localDevice.TryGetProperty("general_product_id", out var pid) ? 
                        pid.GetString() ?? "" : "";

                    report.Bluetooth[name] = new BluetoothInfo
                    {
                        DeviceId = $"{vendorId}-{productId}".Replace("0x", "").ToUpperInvariant()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to parse macOS system profiler output: {Error}", ex.Message);
        }
    }

    private async Task<Dictionary<string, NetworkInfo>?> CollectMacOSNetworkAsync(CancellationToken ct)
    {
        var network = new Dictionary<string, NetworkInfo>();

        // Use ioreg to get PCI network devices
        var output = await RunCommandAsync("ioreg", "-r -c IOPCIDevice -a", ct);

        // Parse XML plist output to find network controllers
        // This is a simplified version - real implementation would parse the plist properly
        var wifiOutput = await RunCommandAsync("networksetup", "-listallhardwareports", ct);

        foreach (var line in wifiOutput.Split('\n'))
        {
            if (line.StartsWith("Hardware Port:"))
            {
                var portName = line.Replace("Hardware Port:", "").Trim();
                if (!portName.Contains("Thunderbolt") && !portName.Contains("iPhone"))
                {
                    network[portName] = new NetworkInfo
                    {
                        DeviceId = "",
                        BusType = portName.Contains("Wi-Fi") ? "PCI" : "PCI"
                    };
                }
            }
        }

        return network.Count > 0 ? network : null;
    }
}
