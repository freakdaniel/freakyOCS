using InfiniFrame;
using InfiniFrame.WebServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OcsNet.Core.Bridge;
using OcsNet.Core.HardwareSniffer;
using OcsNet.Core.Services;
using OcsNet.Core.UsbMapper;
using Serilog;
using Serilog.Events;
using System.Drawing;
using System.Net.Sockets;

namespace OcsNet;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // ── Serilog Setup ────────────────────────────────────────────────────────────
        var logPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "freakyOCS",
    "logs",
    "app-.log");

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("InfiniFrame", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext:l} »  {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext:l} »   {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("App starting...");
            Log.Information("Version: 0.1.0");
            Log.Information("Runtime: {Runtime}", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            Log.Information("OS: {OS}", Environment.OSVersion);

            var webviewTestMode = args.Any(a => string.Equals(a, "--webview-test", StringComparison.OrdinalIgnoreCase));

            // ── Builder ──────────────────────────────────────────────────────────────
            InfiniFrameWebApplicationBuilder builder = InfiniFrameWebApplication.CreateBuilder(args);

            builder.WebApp.Services.AddLogging(b =>
            {
                b.ClearProviders();
                b.AddSerilog(Log.Logger, dispose: true);
            });

            builder.WebApp.Services.AddSingleton<MessageRouter>();
            builder.WebApp.Services.AddSingleton<CompatibilityService>();
            builder.WebApp.Services.AddSingleton<HardwareSnifferService>();
            builder.WebApp.Services.AddSingleton<ProcessRunner>();
            builder.WebApp.Services.AddSingleton<AppUtils>();
            builder.WebApp.Services.AddSingleton<SmbiosService>();
            builder.WebApp.Services.AddSingleton<ReportValidatorService>();
            builder.WebApp.Services.AddSingleton<HardwareCustomizerService>();
            builder.WebApp.Services.AddSingleton<WifiProfileExtractorService>();
            builder.WebApp.Services.AddSingleton<AcpiGuruService>();
            builder.WebApp.Services.AddSingleton<DsdtService>();
            builder.WebApp.Services.AddSingleton<KextService>();
            builder.WebApp.Services.AddSingleton<ConfigService>();
            builder.WebApp.Services.AddSingleton<FileGatheringService>();
            builder.WebApp.Services.AddSingleton<DownloadService>();
            builder.WebApp.Services.AddSingleton<GitHubService>();
            builder.WebApp.Services.AddSingleton<HashService>();
            builder.WebApp.Services.AddSingleton(p =>
            {
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "freakyOCS");
                Directory.CreateDirectory(dataDir);
                return new UsbMapperService(dataDir, p.GetService<ILogger<UsbMapperService>>());
            });

            // ── Static-file wwwroot validation ────────────────────────────────────────
            var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

#if DEBUG
            var projectRoot = FindProjectRoot();
            if (projectRoot is not null)
            {
                var devWww = Path.Combine(projectRoot, "wwwroot");
                if (Directory.Exists(devWww) && File.Exists(Path.Combine(devWww, "index.html")))
                {
                    wwwroot = devWww;
                    Log.Debug("Using development wwwroot: {Path}", wwwroot);
                }
            }
#endif

            Log.Information("wwwroot path: {Path}", wwwroot);

            if (!Directory.Exists(wwwroot) || !File.Exists(Path.Combine(wwwroot, "index.html")))
            {
                Log.Error("wwwroot/index.html not found at: {Path}", wwwroot);
                Log.Error("Run 'npm run build' in the Frontend folder first");
                Environment.Exit(1);
            }

            // Tell ASP.NET Core where to find static files
            builder.WebApp.WebHost.UseWebRoot(wwwroot);

            // ── Port selection ────────────────────────────────────────────────────────
            var port = FindFreePort(43123, 43223);
            builder.WebApp.WebHost.UseUrls($"http://localhost:{port}");
            Log.Information("Static file server will listen on port {Port}", port);

            // ── Window ───────────────────────────────────────────────────────────────
            Log.Information("Configuring InfiniFrame window...");

            builder.Window
                .SetTitle("OpCore Simplify")
                .SetSize(new Size(1280, 800))
                .SetMinSize(new Size(1024, 700))
                .Center()
                .SetResizable(true)
                .SetContextMenuEnabled(false)
                .SetDevToolsEnabled(true)
                .SetUseOsDefaultSize(false)
                .GrantBrowserPermissions();

            if (OperatingSystem.IsWindows())
            {
                var existingArgs = Environment.GetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS");
                var userData = Environment.GetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER");

                if (string.IsNullOrWhiteSpace(userData))
                {
                    var appDataDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "freakyOCS",
                        "webview2");
                    Directory.CreateDirectory(appDataDir);
                    Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", appDataDir);
                    Log.Information("WEBVIEW2_USER_DATA_FOLDER set to: {Path}", appDataDir);
                }
            }

            // Service-provider reference captured by the web-message handler closure;
            // assigned after Build() when the DI container is available.
            IServiceProvider? sp = null;

            if (webviewTestMode)
            {
                var testHtml = """
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset="UTF-8">
                        <title>WebView Test</title>
                        <style>
                            html, body {
                                margin: 0;
                                height: 100%;
                                font-family: Segoe UI, sans-serif;
                                background: #ffffff;
                                color: #111111;
                            }
                            body {
                                display: grid;
                                place-items: center;
                            }
                            .card {
                                border: 1px solid #ddd;
                                border-radius: 12px;
                                padding: 24px;
                                width: min(560px, 90vw);
                                box-shadow: 0 8px 30px rgba(0, 0, 0, 0.08);
                            }
                            code {
                                background: #f5f5f5;
                                border-radius: 6px;
                                padding: 2px 6px;
                            }
                        </style>
                    </head>
                    <body>
                        <div class="card">
                            <h2>InfiniFrame WebView test page</h2>
                            <p>If this text is visible, WebView renders correctly.</p>
                            <p>Run without <code>--webview-test</code> to load React app.</p>
                        </div>
                    </body>
                    </html>
                    """;

                builder.Window.SetStartString(testHtml);
                Log.Information("Window configured in --webview-test mode");
            }
            else
            {
                var windowUrl = $"http://localhost:{port}/";

#if DEBUG
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                    var resp = http.GetAsync("http://localhost:5173/").GetAwaiter().GetResult();
                    if (resp.IsSuccessStatusCode)
                    {
                        windowUrl = "http://localhost:5173/";
                        Log.Information("Vite dev server detected, window will use: {Url}", windowUrl);
                    }
                    else
                    {
                        Log.Information("Vite dev server not available, using static files");
                    }
                }
                catch
                {
                    Log.Information("Vite dev server not detected, using static files");
                }
#endif

                builder.Window.SetStartUrl(windowUrl);

                builder.Window.Events.WebMessageReceived += (object? sender, string message) =>
                {
                    Log.Debug("WebMessage received: {Message}",
                        message?.Length > 200 ? message[..200] + "..." : message);
                    if (sp?.GetService<MessageRouter>() is { } router && sender is IInfiniFrameWindow win)
                        router.Handle(win, message);
                };
            }

            InfiniFrameWebApplication application = builder.Build();

            if (!webviewTestMode)
            {
                sp = application.WebApp.Services;
                HandlersSetup.RegisterAll(sp, sp.GetRequiredService<MessageRouter>());
            }

            application.UseAutoServerClose();

            application.WebApp.UseStaticFiles();
            application.WebApp.MapStaticAssets();

            Log.Information("Starting application...");
            application.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "OCS-Net.csproj")) ||
                File.Exists(Path.Combine(dir.FullName, "OCS-Net.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return null;
    }

    static int FindFreePort(int rangeStart, int rangeEnd)
    {
        for (var port = rangeStart; port < rangeEnd; port++)
        {
            try
            {
                using var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch { }
        }

        throw new InvalidOperationException($"No available port found in range {rangeStart}–{rangeEnd}.");
    }
}
