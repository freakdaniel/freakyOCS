using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Services;

namespace OcsNet.Core.HardwareSniffer;

public sealed partial class HardwareSnifferService
{
    private async Task CollectWindowsHardwareAsync(HardwareReport report, CancellationToken ct)
    {
        // Collect CPU info
        report.Cpu = await CollectWindowsCpuAsync(ct);

        // Collect Motherboard info
        report.Motherboard = await CollectWindowsMotherboardAsync(ct);

        // Collect GPU info
        report.Gpus = await CollectWindowsGpusAsync(ct);

        // Collect Audio info
        report.Sound = await CollectWindowsAudioAsync(ct);

        // Collect Network info
        report.Network = await CollectWindowsNetworkAsync(ct);

        // Collect Storage info
        report.StorageControllers = await CollectWindowsStorageAsync(ct);

        // Collect Bluetooth info
        report.Bluetooth = await CollectWindowsBluetoothAsync(ct);
    }

    private async Task<CpuInfo?> CollectWindowsCpuAsync(CancellationToken ct)
    {
        var output = await RunCommandAsync("wmic", "cpu get Name,Manufacturer,NumberOfCores /format:csv", ct);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 2)
            return null;

        var headers = lines[0].Split(',');
        var values = lines[1].Split(',');

        var cpu = new CpuInfo();

        for (int i = 0; i < headers.Length && i < values.Length; i++)
        {
            switch (headers[i].Trim())
            {
                case "Manufacturer":
                    cpu.Manufacturer = values[i].Trim();
                    break;
                case "Name":
                    cpu.ProcessorName = values[i].Trim();
                    break;
                case "NumberOfCores":
                    if (int.TryParse(values[i].Trim(), out var cores))
                        cpu.CoreCount = cores;
                    break;
            }
        }

        // Get SIMD features from CPU flags
        var cpuFlags = await RunCommandAsync("powershell", 
            "-Command \"(Get-CimInstance Win32_Processor).Caption\"", ct);
        
        if (cpuFlags.Contains("SSE4")) cpu.SimdFeatures.Add("SSE4.2");
        if (cpuFlags.Contains("AVX2")) cpu.SimdFeatures.Add("AVX2");
        if (cpuFlags.Contains("AVX512")) cpu.SimdFeatures.Add("AVX512");

        return cpu;
    }

    private async Task<MotherboardInfo?> CollectWindowsMotherboardAsync(CancellationToken ct)
    {
        var output = await RunCommandAsync("wmic", "baseboard get Product,Manufacturer /format:csv", ct);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 2)
            return null;

        var headers = lines[0].Split(',');
        var values = lines[1].Split(',');

        var board = new MotherboardInfo();
        string manufacturer = "", product = "";

        for (int i = 0; i < headers.Length && i < values.Length; i++)
        {
            switch (headers[i].Trim())
            {
                case "Manufacturer":
                    manufacturer = values[i].Trim();
                    break;
                case "Product":
                    product = values[i].Trim();
                    break;
            }
        }

        board.Name = $"{manufacturer} {product}".Trim();

        // Detect laptop vs desktop via chassis type
        var chassisOutput = await RunCommandAsync("wmic", "systemenclosure get ChassisTypes /format:list", ct);
        // Chassis types 8-14 are typically laptops
        if (chassisOutput.Contains("{8}") || chassisOutput.Contains("{9}") || 
            chassisOutput.Contains("{10}") || chassisOutput.Contains("{14}"))
        {
            board.Platform = "Laptop";
        }
        else
        {
            board.Platform = "Desktop";
        }

        return board;
    }

    private async Task<Dictionary<string, GpuInfo>?> CollectWindowsGpusAsync(CancellationToken ct)
    {
        var gpus = new Dictionary<string, GpuInfo>();

        // Use PowerShell to get PCI device info for GPUs
        var output = await RunCommandAsync("powershell", 
            "-Command \"Get-PnpDevice -Class Display | Select-Object -Property FriendlyName,InstanceId,Status | ConvertTo-Json\"", ct);

        if (string.IsNullOrEmpty(output))
            return gpus.Count > 0 ? gpus : null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(output.StartsWith("[") ? output : $"[{output}]");
            
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var friendlyName = element.GetProperty("FriendlyName").GetString() ?? "Unknown GPU";
                var instanceId = element.GetProperty("InstanceId").GetString() ?? "";

                var match = PciIdRegex().Match(instanceId);
                if (!match.Success) continue;

                var vendorId = match.Groups[1].Value.ToUpperInvariant();
                var deviceId = match.Groups[2].Value.ToUpperInvariant();
                var fullDeviceId = $"{vendorId}-{deviceId}";

                var gpu = new GpuInfo
                {
                    DeviceId = fullDeviceId,
                    Manufacturer = vendorId switch
                    {
                        "8086" => "Intel",
                        "1002" => "AMD",
                        "10DE" => "NVIDIA",
                        _ => "Unknown"
                    },
                    DeviceType = IsIntegratedGpu(vendorId, deviceId) ? "Integrated GPU" : "Discrete GPU"
                };

                gpus[friendlyName] = gpu;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to parse GPU info: {Error}", ex.Message);
        }

        return gpus.Count > 0 ? gpus : null;
    }

    private async Task<Dictionary<string, AudioInfo>?> CollectWindowsAudioAsync(CancellationToken ct)
    {
        var audio = new Dictionary<string, AudioInfo>();

        var output = await RunCommandAsync("powershell",
            "-Command \"Get-PnpDevice -Class AudioEndpoint,MEDIA | Where-Object {$_.Status -eq 'OK'} | Select-Object -Property FriendlyName,InstanceId | ConvertTo-Json\"", ct);

        if (string.IsNullOrEmpty(output))
            return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(output.StartsWith("[") ? output : $"[{output}]");

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var friendlyName = element.GetProperty("FriendlyName").GetString() ?? "Unknown Audio";
                var instanceId = element.GetProperty("InstanceId").GetString() ?? "";

                var match = PciIdRegex().Match(instanceId);
                if (!match.Success)
                {
                    match = UsbIdRegex().Match(instanceId);
                    if (!match.Success) continue;
                }

                var vendorId = match.Groups[1].Value.ToUpperInvariant();
                var deviceId = match.Groups[2].Value.ToUpperInvariant();
                var fullDeviceId = $"{vendorId}-{deviceId}";

                if (!audio.ContainsKey(friendlyName))
                {
                    audio[friendlyName] = new AudioInfo
                    {
                        DeviceId = fullDeviceId,
                        BusType = instanceId.Contains("USB") ? "USB" : "PCI"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to parse audio info: {Error}", ex.Message);
        }

        return audio.Count > 0 ? audio : null;
    }

    private async Task<Dictionary<string, NetworkInfo>?> CollectWindowsNetworkAsync(CancellationToken ct)
    {
        var network = new Dictionary<string, NetworkInfo>();

        var output = await RunCommandAsync("powershell",
            "-Command \"Get-PnpDevice -Class Net | Where-Object {$_.Status -eq 'OK'} | Select-Object -Property FriendlyName,InstanceId | ConvertTo-Json\"", ct);

        if (string.IsNullOrEmpty(output))
            return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(output.StartsWith("[") ? output : $"[{output}]");

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var friendlyName = element.GetProperty("FriendlyName").GetString() ?? "Unknown Network";
                var instanceId = element.GetProperty("InstanceId").GetString() ?? "";

                // Skip virtual adapters
                if (friendlyName.Contains("Virtual") || friendlyName.Contains("WAN Miniport"))
                    continue;

                var match = PciIdRegex().Match(instanceId);
                string fullDeviceId;
                string busType = "PCI";

                if (match.Success)
                {
                    var vendorId = match.Groups[1].Value.ToUpperInvariant();
                    var deviceId = match.Groups[2].Value.ToUpperInvariant();
                    fullDeviceId = $"{vendorId}-{deviceId}";
                }
                else
                {
                    match = UsbIdRegex().Match(instanceId);
                    if (!match.Success) continue;

                    var vendorId = match.Groups[1].Value.ToUpperInvariant();
                    var deviceId = match.Groups[2].Value.ToUpperInvariant();
                    fullDeviceId = $"{vendorId}-{deviceId}";
                    busType = "USB";
                }

                network[friendlyName] = new NetworkInfo
                {
                    DeviceId = fullDeviceId,
                    BusType = busType
                };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to parse network info: {Error}", ex.Message);
        }

        return network.Count > 0 ? network : null;
    }

    private async Task<Dictionary<string, StorageInfo>?> CollectWindowsStorageAsync(CancellationToken ct)
    {
        var storage = new Dictionary<string, StorageInfo>();

        var output = await RunCommandAsync("powershell",
            "-Command \"Get-PnpDevice -Class SCSIAdapter,hdc | Where-Object {$_.Status -eq 'OK'} | Select-Object -Property FriendlyName,InstanceId | ConvertTo-Json\"", ct);

        if (string.IsNullOrEmpty(output))
            return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(output.StartsWith("[") ? output : $"[{output}]");

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var friendlyName = element.GetProperty("FriendlyName").GetString() ?? "Unknown Storage";
                var instanceId = element.GetProperty("InstanceId").GetString() ?? "";

                var match = PciIdRegex().Match(instanceId);
                if (!match.Success) continue;

                var vendorId = match.Groups[1].Value.ToUpperInvariant();
                var deviceId = match.Groups[2].Value.ToUpperInvariant();
                var fullDeviceId = $"{vendorId}-{deviceId}";

                var busType = friendlyName.Contains("NVMe") ? "NVMe" :
                              friendlyName.Contains("AHCI") ? "SATA" :
                              friendlyName.Contains("RAID") ? "RAID" : "SATA";

                storage[friendlyName] = new StorageInfo
                {
                    DeviceId = fullDeviceId,
                    BusType = busType,
                    SubsystemId = ""
                };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to parse storage info: {Error}", ex.Message);
        }

        return storage.Count > 0 ? storage : null;
    }

    private async Task<Dictionary<string, BluetoothInfo>?> CollectWindowsBluetoothAsync(CancellationToken ct)
    {
        var bluetooth = new Dictionary<string, BluetoothInfo>();

        var output = await RunCommandAsync("powershell",
            "-Command \"Get-PnpDevice -Class Bluetooth | Where-Object {$_.Status -eq 'OK'} | Select-Object -Property FriendlyName,InstanceId | ConvertTo-Json\"", ct);

        if (string.IsNullOrEmpty(output))
            return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(output.StartsWith("[") ? output : $"[{output}]");

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var friendlyName = element.GetProperty("FriendlyName").GetString() ?? "Unknown Bluetooth";
                var instanceId = element.GetProperty("InstanceId").GetString() ?? "";

                var match = UsbIdRegex().Match(instanceId);
                if (!match.Success) continue;

                var vendorId = match.Groups[1].Value.ToUpperInvariant();
                var deviceId = match.Groups[2].Value.ToUpperInvariant();
                var fullDeviceId = $"{vendorId}-{deviceId}";

                bluetooth[friendlyName] = new BluetoothInfo
                {
                    DeviceId = fullDeviceId
                };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to parse Bluetooth info: {Error}", ex.Message);
        }

        return bluetooth.Count > 0 ? bluetooth : null;
    }

    private static bool IsIntegratedGpu(string vendorId, string deviceId)
    {
        // Intel GPUs are always integrated (until Arc)
        if (vendorId == "8086")
        {
            // Intel Arc discrete GPUs start with 56xx
            return !deviceId.StartsWith("56");
        }

        // AMD APUs have specific device ID ranges
        if (vendorId == "1002")
        {
            // Vega integrated (Raven Ridge, etc)
            if (deviceId.StartsWith("15") || deviceId.StartsWith("16"))
                return true;
        }

        return false;
    }
}
