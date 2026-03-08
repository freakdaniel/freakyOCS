using System.Text.Json;
using Claunia.PropertyList;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.Services;

public sealed class DownloadService
{
    private const int MaxAttempts = 3;

    private readonly HttpClient _http;
    private readonly HashService _hash;
    private readonly ILogger<DownloadService> _logger;

    public DownloadService(ILogger<DownloadService> logger, HashService? hash = null)
    {
        _logger = logger;
        _hash = hash ?? new HashService();
        _http = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
    }

    public async Task<string?> FetchTextAsync(string url, CancellationToken ct = default)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                var response = await _http.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex) when (attempt < MaxAttempts - 1)
            {
                _logger.LogWarning("Fetch attempt {Attempt} failed for {Url}: {Error}", attempt + 1, url, ex.Message);
            }
        }
        _logger.LogError("Failed to fetch {Url} after {Max} attempts", url, MaxAttempts);
        return null;
    }

    public async Task<T?> FetchJsonAsync<T>(string url, CancellationToken ct = default)
    {
        var content = await FetchTextAsync(url, ct);
        if (content is null) return default;
        try { return JsonSerializer.Deserialize<T>(content); }
        catch (Exception ex)
        {
            _logger.LogError("JSON parse error for {Url}: {Error}", url, ex.Message);
            return default;
        }
    }

    public async Task<NSDictionary?> FetchPlistAsync(string url, CancellationToken ct = default)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                var bytes = await _http.GetByteArrayAsync(url, ct);
                return (NSDictionary)PropertyListParser.Parse(bytes);
            }
            catch (Exception ex) when (attempt < MaxAttempts - 1)
            {
                _logger.LogWarning("Plist fetch attempt {Attempt} failed for {Url}: {Error}", attempt + 1, url, ex.Message);
            }
        }
        return null;
    }

    public async Task<bool> DownloadFileAsync(
        string url,
        string destinationPath,
        string? sha256 = null,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;
                var tempPath = destinationPath + ".tmp";

                await using (var dest = File.Create(tempPath))
                await using (var src = await response.Content.ReadAsStreamAsync(ct))
                {
                    var buffer = new byte[16 * 1024];
                    long downloaded = 0;
                    var startTime = DateTime.UtcNow;
                    var lastTime = startTime;
                    long lastBytes = 0;
                    var speeds = new Queue<double>();

                    int read;
                    while ((read = await src.ReadAsync(buffer, ct)) > 0)
                    {
                        await dest.WriteAsync(buffer.AsMemory(0, read), ct);
                        downloaded += read;

                        var now = DateTime.UtcNow;
                        var elapsed = (now - lastTime).TotalSeconds;
                        if (elapsed > 0.5)
                        {
                            var speed = (downloaded - lastBytes) / elapsed;
                            speeds.Enqueue(speed);
                            if (speeds.Count > 5) speeds.Dequeue();
                            var avgSpeed = speeds.Average();

                            progress?.Report(new DownloadProgress(
                                downloaded, totalBytes, avgSpeed));

                            lastTime = now;
                            lastBytes = downloaded;
                        }
                    }
                }

                if (new FileInfo(tempPath).Length == 0)
                {
                    File.Delete(tempPath);
                    continue;
                }

                if (sha256 is not null)
                {
                    _logger.LogInformation("Verifying SHA256...");
                    var computed = _hash.GetSha256(tempPath);
                    if (!string.Equals(computed, sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("SHA256 mismatch. Expected {Expected}, got {Computed}", sha256, computed);
                        File.Delete(tempPath);
                        continue;
                    }
                    _logger.LogInformation("Checksum verified.");
                }

                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                File.Move(tempPath, destinationPath);
                return true;
            }
            catch (Exception ex) when (attempt < MaxAttempts - 1)
            {
                _logger.LogWarning("Download attempt {Attempt} failed for {Url}: {Error}", attempt + 1, url, ex.Message);
            }
        }

        _logger.LogError("Failed to download {Url} after {Max} attempts", url, MaxAttempts);
        return false;
    }
}

public sealed record DownloadProgress(long BytesDownloaded, long? TotalBytes, double SpeedBytesPerSecond)
{
    public int? Percent => TotalBytes.HasValue ? (int)(BytesDownloaded * 100 / TotalBytes.Value) : null;

    public string SpeedString => SpeedBytesPerSecond < 1024 * 1024
        ? $"{SpeedBytesPerSecond / 1024:F1} KB/s"
        : $"{SpeedBytesPerSecond / (1024 * 1024):F1} MB/s";
}
