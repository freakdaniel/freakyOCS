using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Datasets;
using OcsNet.Core.HardwareSniffer;
using OcsNet.Core.Services;
using OcsNet.Core.UsbMapper;
using Serilog;

namespace OcsNet.Core.Bridge;

public static class HandlersSetup
{
    public static void RegisterAll(IServiceProvider sp, MessageRouter router)
    {
        // ── Hardware ─────────────────────────────────────────────────────────────

        router.Register("hardware:detect", async (_, window, requestId) =>
        {
            Log.Information("hardware:detect — starting collection");
            var sniffer = sp.GetRequiredService<HardwareSnifferService>();
            var report = await sniffer.CollectHardwareAsync();
            Log.Information("hardware:detect — done");
            window.Send(AppResponse.Ok("hardware:detected", HardwareFrontendReport.From(report), requestId));

            // Save report JSON to app data folder (%LOCALAPPDATA%/freakyOCS/reports/)
            try
            {
                var reportsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "freakyOCS", "reports");
                Directory.CreateDirectory(reportsDir);
                var timestamp  = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var reportPath = Path.Combine(reportsDir, $"report-{timestamp}.json");
                await sniffer.ExportReportAsync(report, reportPath);
                Log.Information("hardware:detect — report saved to {Path}", reportPath);
                window.Send(AppResponse.Ok("hardware:report-saved", reportPath, requestId));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "hardware:detect — failed to save report to disk");
            }
        });

        router.Register("hardware:load-report", async (payload, window, requestId) =>
        {
            var path = payload?.TryGetProperty("path", out var p) == true ? p.GetString() : null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                window.Send(AppResponse.Fail("hardware:loaded", "File not found", requestId));
                return;
            }
            var sniffer = sp.GetRequiredService<HardwareSnifferService>();
            var report = await sniffer.ImportReportAsync(path);
            if (report is null)
            {
                window.Send(AppResponse.Fail("hardware:loaded", "Failed to parse report", requestId));
                return;
            }
            window.Send(AppResponse.Ok("hardware:loaded", HardwareFrontendReport.From(report), requestId));
        });

        // ── Compatibility ─────────────────────────────────────────────────────────

        router.Register("compatibility:check", (payload, window, requestId) =>
        {
            var compat = sp.GetRequiredService<CompatibilityService>();
            var report = RebuildReport(payload);

            var devices = new List<object>();
            var warnings = new List<string>();
            var blockers = new List<string>();

            if (report.Cpu is not null)
            {
                var r = compat.CheckCpuCompatibility(report.Cpu, OsData.GetLatestDarwinVersion());
                if (r is null || r.Value.Min is null)
                {
                    blockers.Add($"CPU is not supported by macOS");
                    devices.Add(Device("CPU", report.Cpu.ProcessorName, "unsupported", "Not supported by macOS"));
                }
                else
                {
                    devices.Add(Device("CPU", report.Cpu.ProcessorName, "supported",
                        $"{DarwinToName(r.Value.Min)} — {DarwinToName(r.Value.Max)}", r.Value.Min, r.Value.Max));
                }
            }

            if (report.Gpus is not null)
            {
                foreach (var (name, gpu) in report.Gpus)
                {
                    var r = compat.CheckGpuCompatibility(gpu);
                    if (r is null || r.Value.Min is null)
                    {
                        warnings.Add($"GPU \"{name}\" may need OCLP or additional kexts");
                        devices.Add(Device("GPU", name, "limited", "May require OCLP"));
                    }
                    else
                    {
                        devices.Add(Device("GPU", name, "supported",
                            $"{DarwinToName(r.Value.Min)} — {DarwinToName(r.Value.Max)}", r.Value.Min, r.Value.Max));
                    }
                }
            }

            if (report.Sound is not null)
            {
                foreach (var (name, audio) in report.Sound)
                {
                    var r = compat.CheckAudioCompatibility(audio);
                    if (r?.Min is not null)
                        devices.Add(Device("Audio", name, "supported",
                            $"{DarwinToName(r.Value.Min)} — {DarwinToName(r.Value.Max)}", r.Value.Min, r.Value.Max));
                    else
                    {
                        warnings.Add($"Audio \"{name}\" requires AppleALC kext");
                        devices.Add(Device("Audio", name, "limited", "Requires AppleALC kext"));
                    }
                }
            }

            if (report.Network is not null)
            {
                foreach (var (name, net) in report.Network)
                {
                    var r = compat.CheckNetworkCompatibility(net);
                    if (r?.Min is not null)
                        devices.Add(Device("Network", name, "supported",
                            $"{DarwinToName(r.Value.Min)} — {DarwinToName(r.Value.Max)}", r.Value.Min, r.Value.Max));
                    else
                    {
                        warnings.Add($"Network \"{name}\" may require a kext");
                        devices.Add(Device("Network", name, "limited", "May require kext"));
                    }
                }
            }

            var overallStatus = blockers.Count > 0 ? "unsupported"
                : warnings.Count > 0 ? "limited" : "supported";

            window.Send(AppResponse.Ok("compatibility:result",
                new { devices, overallStatus, warnings, blockers }, requestId));
            return Task.CompletedTask;
        });

        // ── macOS versions ────────────────────────────────────────────────────────

        router.Register("macos:list", (payload, window, requestId) =>
        {
            var versions = OsData.MacosVersions.Select(v => new
            {
                name = v.Name,
                version = v.MacosVersion,
                darwin = $"{v.DarwinVersion}.0.0",
                supported = true,
            }).ToArray();
            window.Send(AppResponse.Ok("macos:versions", versions, requestId));
            return Task.CompletedTask;
        });

        // ── ACPI ──────────────────────────────────────────────────────────────────

        router.Register("acpi:list", (payload, window, requestId) =>
        {
            var report = RebuildReport(payload);
            var patches = AcpiPatchData.Patches.Select(p =>
            {
                var (enabled, category) = GetAcpiPatchSuggestion(p.Name, report);
                return new
                {
                    id = p.FunctionName,
                    name = p.Name,
                    description = p.Description,
                    category,
                    enabled,
                    required = category == "Required",
                    fileName = p.FunctionName,
                };
            }).ToArray();
            window.Send(AppResponse.Ok("acpi:patches", patches, requestId));
            return Task.CompletedTask;
        });

        // ── Kexts ─────────────────────────────────────────────────────────────────

        router.Register("kexts:list", (payload, window, requestId) =>
        {
            var report = RebuildReport(payload);
            var macosVersion = GetMacosVersion(payload) ?? OsData.GetLatestDarwinVersion();
            HashSet<string>? autoKexts = null;
            if (report.Cpu is not null)
                autoKexts = sp.GetRequiredService<KextService>().SelectRequiredKexts(report, macosVersion);

            var kexts = KextData.Kexts.Select(k => new
            {
                id = k.Name.ToLowerInvariant().Replace(" ", "_"),
                name = k.Name,
                version = "latest",
                description = k.Description,
                category = k.Category,
                enabled = autoKexts?.Contains(k.Name) ?? k.Required,
                required = k.Required,
                dependencies = k.RequiresKexts,
                minMacOS = k.MinDarwinVersion,
                maxMacOS = k.MaxDarwinVersion,
            }).ToArray();
            window.Send(AppResponse.Ok("kexts:list", kexts, requestId));
            return Task.CompletedTask;
        });

        // ── SMBIOS ────────────────────────────────────────────────────────────────

        router.Register("smbios:list", (payload, window, requestId) =>
        {
            var report = RebuildReport(payload);
            var macosVersion = GetMacosVersion(payload) ?? OsData.GetLatestDarwinVersion();
            var darwin = OsData.ParseDarwinVersion(macosVersion);
            var isLaptop = (report.Motherboard?.Platform ?? "Desktop") == "Laptop";
            string? recommended = null;
            if (report.Cpu is not null)
                recommended = sp.GetRequiredService<SmbiosService>().SelectSmbiosModel(report, macosVersion);

            var models = MacModelData.MacDevices.Select(m =>
            {
                var isModelLaptop = m.Name.StartsWith("MacBook");
                var hidden = (isLaptop && !isModelLaptop) || (!isLaptop && isModelLaptop);
                var minDarwin = OsData.ParseDarwinVersion(m.InitialSupport);
                var maxDarwin = OsData.ParseDarwinVersion(m.EffectiveLastSupported);
                var compatible = !hidden &&
                                 darwin.Major >= minDarwin.Major &&
                                 darwin.Major <= maxDarwin.Major;
                return new
                {
                    id = m.Name,
                    name = m.Name,
                    year = GenerationYear(m.CpuGeneration),
                    cpuFamily = m.CpuGeneration,
                    gpuFamily = m.DiscreteGpu ?? "Integrated",
                    recommended = m.Name == recommended,
                    minMacOS = m.InitialSupport,
                    maxMacOS = m.LastSupportedVersion,
                    compatible,
                    hidden,
                };
            }).ToArray();
            window.Send(AppResponse.Ok("smbios:models", models, requestId));
            return Task.CompletedTask;
        });

        router.Register("smbios:generate", async (payload, window, requestId) =>
        {
            var model = payload?.TryGetProperty("model", out var mp) == true
                ? mp.GetString() ?? "iMacPro1,1" : "iMacPro1,1";
            var data = await sp.GetRequiredService<SmbiosService>().GenerateSmbiosAsync(model);
            window.Send(AppResponse.Ok("smbios:generated", new
            {
                model = data.SystemProductName,
                serial = data.SystemSerialNumber,
                mlb = data.MLB,
                uuid = data.SystemUUID,
                rom = data.ROM,
            }, requestId));
        });

        // ── USB Mapper ────────────────────────────────────────────────────────────

        router.Register("usb:scan", async (_, window, requestId) =>
        {
            var usb = sp.GetRequiredService<UsbMapperService>();
            var controllers = await usb.GetControllersAsync();
            window.Send(AppResponse.Ok("usb:controllers", MapControllers(controllers), requestId));
        });

        router.Register("usb:toggle-port", (payload, window, requestId) =>
        {
            var usb = sp.GetRequiredService<UsbMapperService>();
            var ci  = payload?.TryGetProperty("controller", out var c) == true ? c.GetInt32() : -1;
            var pi  = payload?.TryGetProperty("port",       out var p) == true ? p.GetInt32() : -1;
            if (ci >= 0 && pi >= 0) usb.TogglePortAt(ci, pi);
            window.Send(AppResponse.Ok("usb:updated", MapControllers(usb.ControllersHistorical ?? []), requestId));
            return Task.CompletedTask;
        });

        router.Register("usb:set-type", (payload, window, requestId) =>
        {
            var usb = sp.GetRequiredService<UsbMapperService>();
            var ci  = payload?.TryGetProperty("controller", out var c) == true ? c.GetInt32() : -1;
            var pi  = payload?.TryGetProperty("port",       out var p) == true ? p.GetInt32() : -1;
            var ct  = payload?.TryGetProperty("type",       out var t) == true ? t.GetInt32() : 0;
            if (ci >= 0 && pi >= 0) usb.SetPortTypeAt(ci, pi, (UsbConnectorType)ct);
            window.Send(AppResponse.Ok("usb:updated", MapControllers(usb.ControllersHistorical ?? []), requestId));
            return Task.CompletedTask;
        });

        // ── Build ─────────────────────────────────────────────────────────────────

        CancellationTokenSource? buildCts = null;
        var buildCtsLock = new object();

        router.Register("build:start", async (payload, window, requestId) =>
        {
            // Acknowledge immediately so the frontend invoke() resolves before the build completes
            window.Send(AppResponse.Ok("build:started", null, requestId));

            // Cancel any already-running build and create a fresh CTS
            CancellationTokenSource cts;
            lock (buildCtsLock)
            {
                buildCts?.Cancel();
                buildCts = cts = new CancellationTokenSource();
            }

            var build  = sp.GetRequiredService<BuildService>();
            var report = RebuildReport(payload);

            // SMBIOS
            SmbiosData? smbios = null;
            if (payload?.TryGetProperty("smbios", out var smEl) == true)
                smbios = new SmbiosData(
                    MLB:                Get(smEl, "mlb"),
                    ROM:                Get(smEl, "rom"),
                    SystemProductName:  Get(smEl, "model"),
                    SystemSerialNumber: Get(smEl, "serial"),
                    SystemUUID:         Get(smEl, "uuid"));

            if (smbios is null)
            {
                window.Send(AppResponse.Fail("build:error", "SMBIOS not configured", null));
                return;
            }

            var macosVersion = GetMacosVersion(payload) ?? OsData.GetLatestDarwinVersion();

            // Enabled kext names
            var kextNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (payload?.TryGetProperty("kexts", out var kextsEl) == true)
                foreach (var k in kextsEl.EnumerateArray())
                    if (k.TryGetProperty("enabled", out var en) && en.GetBoolean())
                    {
                        var n = Get(k, "name");
                        if (!string.IsNullOrEmpty(n)) kextNames.Add(n);
                    }

            // Enabled ACPI patch IDs
            var acpiPatchIds = new List<string>();
            if (payload?.TryGetProperty("acpiPatches", out var patchesEl) == true)
                foreach (var p in patchesEl.EnumerateArray())
                    if (p.TryGetProperty("enabled", out var en) && en.GetBoolean())
                    {
                        var id = Get(p, "id");
                        if (!string.IsNullOrEmpty(id)) acpiPatchIds.Add(id);
                    }

            // USB controllers — use the stored service state (the user already configured this)
            var usbSvc = sp.GetRequiredService<UsbMapperService>();
            var usbControllers = usbSvc.ControllersHistorical ?? [];

            _ = Task.Run(async () =>
            {
                try
                {
                    var outputPath = await build.BuildAsync(
                        report, smbios, macosVersion, kextNames, acpiPatchIds, usbControllers,
                        sendProgress: (stage, pct, msg) =>
                            window.Send(AppResponse.Ok("build:progress", new
                            {
                                stage,
                                progress = pct,
                                message  = msg,
                                log      = Array.Empty<string>(),
                            }, null)),
                        cts.Token);

                    window.Send(AppResponse.Ok("build:complete", new
                    {
                        success      = true,
                        outputPath,
                        biosSettings = Array.Empty<object>(),
                        nextSteps    = Array.Empty<string>(),
                    }, null));
                }
                catch (OperationCanceledException)
                {
                    window.Send(AppResponse.Fail("build:error", "Build was cancelled", null));
                }
                catch (Exception ex)
                {
                    window.Send(AppResponse.Fail("build:error", ex.Message, null));
                }
            });
        });

        router.Register("build:cancel", (_, window, requestId) =>
        {
            lock (buildCtsLock)
            {
                buildCts?.Cancel();
                buildCts = null;
            }
            window.Send(AppResponse.Ok("build:cancelled", null, requestId));
            return Task.CompletedTask;
        });

        // ── Result ────────────────────────────────────────────────────────────────

        router.Register("result:open-folder", (payload, window, requestId) =>
        {
            var path = payload?.TryGetProperty("path", out var pp) == true ? pp.GetString() : null;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Process.Start("explorer.exe", path);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    Process.Start("xdg-open", path);
                else
                    Process.Start("open", path);
            }
            window.Send(AppResponse.Ok("result:opened", null, requestId));
            return Task.CompletedTask;
        });

        // ── Report Validation ─────────────────────────────────────────────────────

        router.Register("report:validate", (payload, window, requestId) =>
        {
            var validator = sp.GetRequiredService<ReportValidatorService>();
            var json = payload?.GetRawText() ?? "{}";
            if (payload?.TryGetProperty("report", out var reportEl) == true)
                json = reportEl.GetRawText();

            var (isValid, errors, warnings, _) = validator.ValidateReport(json);
            window.Send(AppResponse.Ok("report:validated", new
            {
                isValid,
                errors,
                warnings,
            }, requestId));
            return Task.CompletedTask;
        });

        // ── Hardware Customization ────────────────────────────────────────────────

        router.Register("customize:check", (payload, window, requestId) =>
        {
            var customizer = sp.GetRequiredService<HardwareCustomizerService>();
            var report = RebuildReport(payload);
            var macosVersion = payload?.TryGetProperty("macosVersion", out var mv) == true
                ? mv.GetString() ?? "24.0.0" : "24.0.0";

            var result = customizer.Customize(report, macosVersion);
            window.Send(AppResponse.Ok("customize:result", new
            {
                compatibleGpus = result.CompatibleGpus,
                compatibleWifi = result.CompatibleWifi,
                disabledDevices = result.DisabledDevices,
                needsOclp = result.NeedsOclp,
                hasMultipleGpus = result.HasMultipleGpus,
                hasMultipleWifi = result.HasMultipleWifi,
                gpuConflictWarning = result.GpuConflictWarning,
            }, requestId));
            return Task.CompletedTask;
        });

        // ── WiFi Profiles ─────────────────────────────────────────────────────────

        router.Register("wifi:scan", async (_, window, requestId) =>
        {
            var wifi = sp.GetRequiredService<WifiProfileExtractorService>();
            var profiles = await wifi.GetProfilesAsync();
            window.Send(AppResponse.Ok("wifi:profiles", profiles.Select(p => new
            {
                ssid = p.Ssid,
                password = p.Password,
            }).ToArray(), requestId));
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reconstructs an internal HardwareReport from the frontend DTO JSON.
    /// Supports both raw payload and payload wrapped in {report: ...}.
    /// </summary>
    private static HardwareReport RebuildReport(JsonElement? payload)
    {
        var report = new HardwareReport();
        if (payload is null) return report;

        var root = payload.Value;
        if (root.TryGetProperty("report", out var inner)) root = inner;

        if (root.TryGetProperty("cpu", out var cpuEl))
        {
            report.Cpu = new CpuInfo
            {
                ProcessorName = Get(cpuEl, "name"),
                Manufacturer = Get(cpuEl, "vendor") switch
                {
                    "Intel" => "GenuineIntel",
                    "AMD"   => "AuthenticAMD",
                    var v   => v
                },
                Codename = Get(cpuEl, "codename"),
                CoreCount = GetInt(cpuEl, "cores"),
                ThreadCount = GetInt(cpuEl, "threads"),
            };
            if (cpuEl.TryGetProperty("supportedFeatures", out var feats))
                foreach (var f in feats.EnumerateArray())
                    report.Cpu.SimdFeatures.Add(f.GetString() ?? "");
        }

        if (root.TryGetProperty("gpu", out var gpuArr))
        {
            report.Gpus = [];
            foreach (var g in gpuArr.EnumerateArray())
            {
                var name = Get(g, "name", "GPU");
                report.Gpus[name] = new GpuInfo
                {
                    Manufacturer = Get(g, "vendor"),
                    DeviceId     = Get(g, "deviceId"),
                    DeviceType   = g.TryGetProperty("discrete", out var d) && d.GetBoolean()
                        ? "Discrete GPU" : "Integrated GPU",
                };
            }
        }

        if (root.TryGetProperty("audio", out var audioArr))
        {
            report.Sound = [];
            foreach (var a in audioArr.EnumerateArray())
            {
                var name = Get(a, "name", "Audio");
                report.Sound[name] = new AudioInfo
                {
                    DeviceId = $"{Get(a, "vendorId")}-{Get(a, "deviceId")}",
                };
            }
        }

        if (root.TryGetProperty("network", out var netArr))
        {
            report.Network = [];
            foreach (var n in netArr.EnumerateArray())
            {
                var name = Get(n, "name", "NIC");
                report.Network[name] = new NetworkInfo
                {
                    DeviceId = $"{Get(n, "vendorId")}-{Get(n, "deviceId")}",
                };
            }
        }

        if (root.TryGetProperty("motherboard", out var mbEl))
        {
            var mfr   = Get(mbEl, "manufacturer");
            var model = Get(mbEl, "model");
            report.Motherboard = new MotherboardInfo { Name = $"{mfr} {model}".Trim() };
        }

        return report;
    }

    private static object Device(string category, string name, string status, string notes,
        string? min = null, string? max = null)
        => new { category, name, status, notes, minMacOS = min, maxMacOS = max };

    private static string DarwinToName(string? darwin)
        => darwin is null ? "Unknown" : OsData.GetMacosNameByDarwin(darwin) ?? darwin;

    private static object[] MapControllers(IEnumerable<UsbController> controllers) =>
        [.. controllers.Select(c => new
        {
            name = c.Name,
            type = c.ControllerType.ToString(),
            ports = (object[])[.. c.Ports.Select(p => new
            {
                name = p.Name,
                index = p.Index,
                speedClass = p.SpeedClass.ToString(),
                connectorType = (int?)p.ConnectorType,
                guessedType = (int?)p.GuessedType,
                devices = (object[])[.. p.Devices.Select(d =>
                    new { name = d.Name, speed = d.Speed.ToString(), instanceId = d.InstanceId })],
                selected = p.Selected,
                comment = p.Comment,
            })],
            selectedCount = c.SelectedCount,
        })];

    private static int GenerationYear(string gen) => gen switch
    {
        "Lynnfield" or "Clarkdale"    => 2010,
        "Sandy Bridge"                => 2011,
        "Ivy Bridge"                  => 2012,
        "Haswell"                     => 2013,
        "Broadwell"                   => 2015,
        "Skylake"                     => 2016,
        "Kaby Lake" or "Amber Lake"   => 2017,
        "Coffee Lake"                 => 2018,
        "Ice Lake"                    => 2019,
        "Comet Lake"                  => 2020,
        "Tiger Lake"                  => 2021,
        _                             => 2019,
    };

    private static string Get(JsonElement el, string key, string fallback = "")
        => el.TryGetProperty(key, out var v) ? v.GetString() ?? fallback : fallback;

    private static int GetInt(JsonElement el, string key, int fallback = 0)
        => el.TryGetProperty(key, out var v) ? v.GetInt32() : fallback;

    private static string? GetMacosVersion(JsonElement? payload)
    {
        if (payload is null) return null;
        var root = payload.Value;
        if (root.TryGetProperty("macos", out var mv))
        {
            if (mv.TryGetProperty("darwin", out var d)) return d.GetString();
            if (mv.ValueKind == JsonValueKind.String) return mv.GetString();
        }
        return null;
    }

    private static int IntelGeneration(string codename)
    {
        if (codename.Contains("Sandy Bridge")) return 2;
        if (codename.Contains("Ivy Bridge"))   return 3;
        if (codename.Contains("Haswell"))      return 4;
        if (codename.Contains("Broadwell"))    return 5;
        if (codename.Contains("Skylake"))      return 6;
        if (codename.Contains("Kaby Lake") || codename.Contains("Amber Lake")) return 7;
        if (codename.Contains("Coffee Lake") || codename.Contains("Whiskey Lake") ||
            codename.Contains("Cannon Lake"))  return 8;
        if (codename.Contains("Comet Lake"))   return 10;
        if (codename.Contains("Ice Lake"))     return 10;
        if (codename.Contains("Tiger Lake"))   return 11;
        if (codename.Contains("Alder Lake"))   return 12;
        if (codename.Contains("Raptor Lake"))  return 13;
        return 0;
    }

    private static bool IsHedtCpu(string processorName, string codename) =>
        codename.EndsWith("-X")  || codename.EndsWith("-W")  ||
        codename.EndsWith("-E")  || codename.EndsWith("-EP") || codename.EndsWith("-EX") ||
        processorName.Contains("Xeon") ||
        codename.StartsWith("Skylake-X") || codename.StartsWith("Cascade Lake") ||
        codename.StartsWith("Basin Falls") || codename.StartsWith("Broadwell-E");

    private static (bool Enabled, string Category) GetAcpiPatchSuggestion(
        string patchName, HardwareReport report)
    {
        if (report.Cpu is null) return (false, "Optional");

        var isLaptop = (report.Motherboard?.Platform ?? "Desktop") == "Laptop";
        var isIntel  = report.Cpu.Manufacturer?.Contains("Intel") == true;
        var codename = report.Cpu.Codename ?? "";
        var procName = report.Cpu.ProcessorName ?? "";
        var isHedt   = IsHedtCpu(procName, codename);
        var gen      = IntelGeneration(codename);
        var mbName   = report.Motherboard?.Name ?? "";

        return patchName switch
        {
            "PLUG"          => (isIntel && gen >= 4, "Required"),
            "RTCAWAC"       => (isIntel && gen >= 8, "Required"),
            "PMC"           => (isIntel && gen >= 8, "Required"),
            "FakeEC"        => (!isLaptop && isIntel, "Required"),
            "USBX"          => (true, "Required"),
            "PNLF"          => (isLaptop && isIntel, "Required"),
            "GPI0"          => (isLaptop, "Recommended"),
            "XOSI"          => (isLaptop, "Recommended"),
            "IMEI"          => (isIntel && gen is 2 or 3, "Required"),
            "MCHC"          => (isIntel && !isHedt, "Recommended"),
            "BUS0"          => (isIntel, "Recommended"),
            "APIC"          => (isHedt, "Required"),
            "RTC0"          => (isHedt, "Required"),
            "UNC"           => (isHedt, "Required"),
            "FixHPET"       => (isIntel && gen is > 0 and <= 3, "Recommended"),
            "PM (Legacy)"   => (isIntel && gen is > 0 and <= 3, "Required"),
            "BATP"          => (isLaptop, "Required"),
            "PRW"           => (!isLaptop, "Recommended"),
            "ALS"           => (isLaptop, "Optional"),
            "Surface Patch" => (mbName.Contains("Surface"), "Hardware-Specific"),
            "CMOS"          => (mbName.Contains("HP") || mbName.Contains("Hewlett-Packard"), "Hardware-Specific"),
            _               => (false, "Optional"),
        };
    }
}
