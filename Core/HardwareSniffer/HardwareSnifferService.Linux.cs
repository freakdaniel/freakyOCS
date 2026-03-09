using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Services;

namespace OcsNet.Core.HardwareSniffer;

public sealed partial class HardwareSnifferService
{
    private async Task CollectLinuxHardwareAsync(HardwareReport report, CancellationToken ct)
    {
        // CPU and Motherboard are read from the /proc and /sys filesystems (fast, < 10 ms).
        report.Cpu = await CollectLinuxCpuAsync(ct);
        report.Motherboard = await CollectLinuxMotherboardAsync(ct);

        // All PCI / USB queries are run in parallel so the total collection time is
        // bounded by the slowest single command (≤ 8 s per-command timeout) rather than
        // the sum of all commands.  This avoids the 30 s bridge timeout that fires when
        // the 9+ lspci + lsusb calls are executed sequentially.
        var gpuTask       = CollectLinuxGpusAsync(ct);
        var audioTask     = CollectLinuxAudioAsync(ct);
        var networkTask   = CollectLinuxNetworkAsync(ct);
        var storageTask   = CollectLinuxStorageAsync(ct);
        var bluetoothTask = CollectLinuxBluetoothAsync(ct);

        await Task.WhenAll(gpuTask, audioTask, networkTask, storageTask, bluetoothTask);

        report.Gpus             = gpuTask.Result;
        report.Sound            = audioTask.Result;
        report.Network          = networkTask.Result;
        report.StorageControllers = storageTask.Result;
        report.Bluetooth        = bluetoothTask.Result;
    }

    private async Task<CpuInfo?> CollectLinuxCpuAsync(CancellationToken ct)
    {
        var cpuInfo = await File.ReadAllTextAsync("/proc/cpuinfo", ct);
        var cpu = new CpuInfo();
        var featuresSet = false;

        foreach (var line in cpuInfo.Split('\n'))
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case "processor":
                    // Each "processor" entry is one logical CPU (thread)
                    cpu.ThreadCount++;
                    break;
                case "model name":
                    if (cpu.ProcessorName.Length == 0)
                        cpu.ProcessorName = value;
                    break;
                case "vendor_id":
                    if (cpu.Manufacturer.Length == 0)
                        cpu.Manufacturer = value;
                    break;
                case "cpu cores":
                    if (int.TryParse(value, out var cores) && cpu.CoreCount == 0)
                        cpu.CoreCount = cores;
                    break;
                case "flags":
                    if (!featuresSet)
                    {
                        if (value.Contains("sse4_2")) cpu.SimdFeatures.Add("SSE4.2");
                        if (value.Contains("avx2")) cpu.SimdFeatures.Add("AVX2");
                        if (value.Contains("avx512")) cpu.SimdFeatures.Add("AVX512");
                        featuresSet = true;
                    }
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

        // VGA: 0300, 3D controller: 0302 — run both in parallel
        var gpuT1 = RunCommandAsync("lspci", "-nn -d ::0300", ct);
        var gpuT2 = RunCommandAsync("lspci", "-nn -d ::0302", ct);
        await Task.WhenAll(gpuT1, gpuT2);
        var output = gpuT1.Result + "\n" + gpuT2.Result;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = LinuxPciIdRegex().Match(line);
            if (!match.Success) continue;

            var vendorId = match.Groups[1].Value.ToUpperInvariant();
            var deviceId = match.Groups[2].Value.ToUpperInvariant();
            var fullDeviceId = $"{vendorId}-{deviceId}";
            var name = ExtractPciDeviceName(line);

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

        var output = await RunCommandAsync("lspci", "-nn -d ::0403", ct);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = LinuxPciIdRegex().Match(line);
            if (!match.Success) continue;

            var vendorId = match.Groups[1].Value.ToUpperInvariant();
            var deviceId = match.Groups[2].Value.ToUpperInvariant();
            var fullDeviceId = $"{vendorId}-{deviceId}";
            var name = ExtractPciDeviceName(line);

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

        // Ethernet: 0200, WiFi: 0280 — run both in parallel
        var ethT = RunCommandAsync("lspci", "-nn -d ::0200", ct);
        var wifiT = RunCommandAsync("lspci", "-nn -d ::0280", ct);
        await Task.WhenAll(ethT, wifiT);
        var ethernetOut = ethT.Result;
        var wifiOut = wifiT.Result;

        foreach (var (output, netType) in new[] { (ethernetOut, "Ethernet"), (wifiOut, "WiFi") })
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var match = LinuxPciIdRegex().Match(line);
                if (!match.Success) continue;

                var vendorId = match.Groups[1].Value.ToUpperInvariant();
                var deviceId = match.Groups[2].Value.ToUpperInvariant();
                var fullDeviceId = $"{vendorId}-{deviceId}";
                var name = ExtractPciDeviceName(line);

                network[name] = new NetworkInfo
                {
                    DeviceId = fullDeviceId,
                    BusType = netType   // "Ethernet" or "WiFi"
                };
            }
        }

        return network.Count > 0 ? network : null;
    }

    private async Task<Dictionary<string, StorageInfo>?> CollectLinuxStorageAsync(CancellationToken ct)
    {
        var storage = new Dictionary<string, StorageInfo>();

        // SATA: 0106, NVMe: 0108 — run both in parallel
        var sataT = RunCommandAsync("lspci", "-nn -d ::0106", ct);
        var nvmeT = RunCommandAsync("lspci", "-nn -d ::0108", ct);
        await Task.WhenAll(sataT, nvmeT);
        var sataOut = sataT.Result;
        var nvmeOut = nvmeT.Result;

        foreach (var (output, busType) in new[] { (sataOut, "SATA"), (nvmeOut, "NVMe") })
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var match = LinuxPciIdRegex().Match(line);
                if (!match.Success) continue;

                var vendorId = match.Groups[1].Value.ToUpperInvariant();
                var deviceId = match.Groups[2].Value.ToUpperInvariant();
                var fullDeviceId = $"{vendorId}-{deviceId}";
                var name = ExtractPciDeviceName(line);

                storage[name] = new StorageInfo
                {
                    DeviceId = fullDeviceId,
                    BusType = busType,
                    SubsystemId = ""
                };
            }
        }

        return storage.Count > 0 ? storage : null;
    }

    /// <summary>
    /// Extracts the human-readable device name from a lspci -nn output line.
    /// lspci format: [DOMAIN:]BUS:SLOT.FUNC Class Name [CCCC]: Vendor Device Name [VVVV:DDDD] (rev XX)
    /// </summary>
    private static string ExtractPciDeviceName(string line)
    {
        // The "]: " that ends the class code bracket is the boundary between
        // the PCI address+class and the vendor+device description.
        var sep = line.IndexOf("]: ", StringComparison.Ordinal);
        if (sep < 0) return "Unknown";

        var namePart = line[(sep + 3)..];

        // Strip trailing [vid:did] and optional (rev XX)
        namePart = Regex.Replace(namePart,
            @"\s*\[[0-9a-fA-F]{4}:[0-9a-fA-F]{4}\].*$",
            "", RegexOptions.None);

        return namePart.Trim();
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
