using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Services;

namespace OcsNet.Core.HardwareSniffer;

public sealed partial class HardwareSnifferService
{
    private async Task CollectLinuxHardwareAsync(HardwareReport report, CancellationToken ct)
    {
        // Collect CPU info
        report.Cpu = await CollectLinuxCpuAsync(ct);

        // Collect Motherboard info
        report.Motherboard = await CollectLinuxMotherboardAsync(ct);

        // Collect GPU info
        report.Gpus = await CollectLinuxGpusAsync(ct);

        // Collect Audio info
        report.Sound = await CollectLinuxAudioAsync(ct);

        // Collect Network info
        report.Network = await CollectLinuxNetworkAsync(ct);

        // Collect Storage info
        report.StorageControllers = await CollectLinuxStorageAsync(ct);

        // Collect Bluetooth info
        report.Bluetooth = await CollectLinuxBluetoothAsync(ct);
    }

    private async Task<CpuInfo?> CollectLinuxCpuAsync(CancellationToken ct)
    {
        var cpuInfo = await File.ReadAllTextAsync("/proc/cpuinfo", ct);
        var cpu = new CpuInfo();

        foreach (var line in cpuInfo.Split('\n'))
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case "model name":
                    cpu.ProcessorName = value;
                    break;
                case "vendor_id":
                    cpu.Manufacturer = value;
                    break;
                case "cpu cores":
                    if (int.TryParse(value, out var cores))
                        cpu.CoreCount = cores;
                    break;
                case "flags":
                    if (value.Contains("sse4_2")) cpu.SimdFeatures.Add("SSE4.2");
                    if (value.Contains("avx2")) cpu.SimdFeatures.Add("AVX2");
                    if (value.Contains("avx512")) cpu.SimdFeatures.Add("AVX512");
                    break;
            }
        }

        return string.IsNullOrEmpty(cpu.ProcessorName) ? null : cpu;
    }

    private async Task<MotherboardInfo?> CollectLinuxMotherboardAsync(CancellationToken ct)
    {
        var board = new MotherboardInfo();

        try
        {
            var vendor = await File.ReadAllTextAsync("/sys/class/dmi/id/board_vendor", ct);
            var name = await File.ReadAllTextAsync("/sys/class/dmi/id/board_name", ct);
            board.Name = $"{vendor.Trim()} {name.Trim()}";

            // Detect laptop
            var chassisType = await File.ReadAllTextAsync("/sys/class/dmi/id/chassis_type", ct);
            if (int.TryParse(chassisType.Trim(), out var type) && type >= 8 && type <= 14)
            {
                board.Platform = "Laptop";
            }
        }
        catch
        {
            return null;
        }

        return board;
    }

    private async Task<Dictionary<string, GpuInfo>?> CollectLinuxGpusAsync(CancellationToken ct)
    {
        var gpus = new Dictionary<string, GpuInfo>();

        // Use lspci to find VGA/3D controllers
        var output = await RunCommandAsync("lspci", "-nn -d ::0300", ct);
        output += "\n" + await RunCommandAsync("lspci", "-nn -d ::0302", ct);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = LinuxPciIdRegex().Match(line);
            if (!match.Success) continue;

            var vendorId = match.Groups[1].Value.ToUpperInvariant();
            var deviceId = match.Groups[2].Value.ToUpperInvariant();
            var fullDeviceId = $"{vendorId}-{deviceId}";

            // Extract name from the line
            var namePart = line.Split(':').Skip(2).FirstOrDefault()?.Trim() ?? "Unknown GPU";
            var name = Regex.Replace(namePart, @"\[.*?\]", "").Trim();

            gpus[name] = new GpuInfo
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
        }

        return gpus.Count > 0 ? gpus : null;
    }

    private async Task<Dictionary<string, AudioInfo>?> CollectLinuxAudioAsync(CancellationToken ct)
    {
        var audio = new Dictionary<string, AudioInfo>();

        // Use lspci to find audio devices
        var output = await RunCommandAsync("lspci", "-nn -d ::0403", ct);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = LinuxPciIdRegex().Match(line);
            if (!match.Success) continue;

            var vendorId = match.Groups[1].Value.ToUpperInvariant();
            var deviceId = match.Groups[2].Value.ToUpperInvariant();
            var fullDeviceId = $"{vendorId}-{deviceId}";

            var namePart = line.Split(':').Skip(2).FirstOrDefault()?.Trim() ?? "Unknown Audio";
            var name = Regex.Replace(namePart, @"\[.*?\]", "").Trim();

            audio[name] = new AudioInfo
            {
                DeviceId = fullDeviceId,
                BusType = "PCI"
            };
        }

        return audio.Count > 0 ? audio : null;
    }

    private async Task<Dictionary<string, NetworkInfo>?> CollectLinuxNetworkAsync(CancellationToken ct)
    {
        var network = new Dictionary<string, NetworkInfo>();

        // WiFi: 0280, Ethernet: 0200
        var output = await RunCommandAsync("lspci", "-nn -d ::0200", ct);
        output += "\n" + await RunCommandAsync("lspci", "-nn -d ::0280", ct);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = LinuxPciIdRegex().Match(line);
            if (!match.Success) continue;

            var vendorId = match.Groups[1].Value.ToUpperInvariant();
            var deviceId = match.Groups[2].Value.ToUpperInvariant();
            var fullDeviceId = $"{vendorId}-{deviceId}";

            var namePart = line.Split(':').Skip(2).FirstOrDefault()?.Trim() ?? "Unknown Network";
            var name = Regex.Replace(namePart, @"\[.*?\]", "").Trim();

            network[name] = new NetworkInfo
            {
                DeviceId = fullDeviceId,
                BusType = "PCI"
            };
        }

        return network.Count > 0 ? network : null;
    }

    private async Task<Dictionary<string, StorageInfo>?> CollectLinuxStorageAsync(CancellationToken ct)
    {
        var storage = new Dictionary<string, StorageInfo>();

        // SATA: 0106, NVMe: 0108
        var output = await RunCommandAsync("lspci", "-nn -d ::0106", ct);
        output += "\n" + await RunCommandAsync("lspci", "-nn -d ::0108", ct);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = LinuxPciIdRegex().Match(line);
            if (!match.Success) continue;

            var vendorId = match.Groups[1].Value.ToUpperInvariant();
            var deviceId = match.Groups[2].Value.ToUpperInvariant();
            var fullDeviceId = $"{vendorId}-{deviceId}";

            var namePart = line.Split(':').Skip(2).FirstOrDefault()?.Trim() ?? "Unknown Storage";
            var name = Regex.Replace(namePart, @"\[.*?\]", "").Trim();

            var busType = line.Contains("NVMe") || line.Contains("0108") ? "NVMe" : "SATA";

            storage[name] = new StorageInfo
            {
                DeviceId = fullDeviceId,
                BusType = busType,
                SubsystemId = ""
            };
        }

        return storage.Count > 0 ? storage : null;
    }

    private async Task<Dictionary<string, BluetoothInfo>?> CollectLinuxBluetoothAsync(CancellationToken ct)
    {
        var bluetooth = new Dictionary<string, BluetoothInfo>();

        // Use lsusb for Bluetooth (usually USB)
        var output = await RunCommandAsync("lsusb", "", ct);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = LinuxPciIdRegex().Match(line);
            if (!match.Success) continue;

            var vendorId = match.Groups[1].Value.ToUpperInvariant();
            var deviceId = match.Groups[2].Value.ToUpperInvariant();
            var fullDeviceId = $"{vendorId}-{deviceId}";

            var name = line.Split(fullDeviceId).LastOrDefault()?.Trim() ?? "Bluetooth";

            bluetooth[name] = new BluetoothInfo
            {
                DeviceId = fullDeviceId
            };
        }

        return bluetooth.Count > 0 ? bluetooth : null;
    }
}
