using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace OcsNet.Core.Services;

public sealed record GitHubAsset(
    string ProductName,
    int Id,
    string Url,
    string? Sha256
)
{
    // Aliases for compatibility
    public string Name => ProductName;
    public string DownloadUrl => Url;
}

public sealed record GitHubRelease(string Body, GitHubAsset[] Assets);

public sealed record GitHubReleaseInfo(
    string TagName,
    GitHubAsset[] Assets
);

public sealed class GitHubService(DownloadService download, ILogger<GitHubService> logger)
{
    private JsonNode? ExtractPayload(string html)
    {
        foreach (var line in html.Split('\n'))
        {
            if (!line.Contains("type=\"application/json\"")) continue;
            try
            {
                var content = line.Split('>')[1].Split('<')[0];
                var node = JsonNode.Parse(content);
                return node?["payload"];
            }
            catch { }
        }
        return null;
    }

    public async Task<JsonNode?> GetCommitsAsync(string owner, string repo, string branch = "main",
        string? startCommit = null, int after = -1, CancellationToken ct = default)
    {
        string url;
        if (after > -1)
        {
            startCommit ??= (await GetCommitsAsync(owner, repo, branch, null, -1, ct))?["currentCommit"]?["oid"]?.GetValue<string>();
            url = $"https://github.com/{owner}/{repo}/commits/{branch}?after={startCommit}+{after}";
        }
        else
        {
            url = $"https://github.com/{owner}/{repo}/commits/{branch}";
        }

        var html = await download.FetchTextAsync(url, ct);
        if (html is null)
        {
            logger.LogError("Failed to fetch commits for {Owner}/{Repo}", owner, repo);
            return null;
        }

        var payload = ExtractPayload(html);
        if (payload?["commitGroups"] is null)
        {
            logger.LogError("Cannot find commit information for {Repo} on {Branch}", repo, branch);
            return null;
        }

        return payload;
    }

    public async Task<GitHubRelease?> GetLatestReleaseAsync(string owner, string repo, CancellationToken ct = default)
    {
        var url = $"https://github.com/{owner}/{repo}/releases";
        var html = await download.FetchTextAsync(url, ct);
        if (html is null)
        {
            logger.LogError("Failed to fetch releases for {Owner}/{Repo}", owner, repo);
            return null;
        }

        var tagName = ExtractTagName(html);
        if (tagName is null)
        {
            logger.LogWarning("No release tag found for {Owner}/{Repo}", owner, repo);
            return null;
        }

        var body = ExtractBodyContent(html);

        var assetsUrl = $"https://github.com/{owner}/{repo}/releases/expanded_assets/{tagName}";
        var assetsHtml = await download.FetchTextAsync(assetsUrl, ct);
        if (assetsHtml is null)
        {
            logger.LogError("Failed to fetch expanded assets for {Owner}/{Repo}", owner, repo);
            return null;
        }

        var assets = ExtractAssets(assetsHtml);
        return new GitHubRelease(body, assets);
    }

    private static string? ExtractTagName(string html)
    {
        foreach (var line in html.Split('\n'))
        {
            if (line.Contains("<a") && line.Contains("href=\"") && line.Contains("/releases/tag/"))
            {
                var parts = line.Split("/releases/tag/");
                if (parts.Length < 2) continue;
                return parts[1].Split('"')[0];
            }
        }
        return null;
    }

    private static string ExtractBodyContent(string html)
    {
        foreach (var line in html.Split('\n'))
        {
            if (line.Contains("<div") && line.Contains("body-content"))
            {
                var prefix = line.Split('>')[0];
                var after = html.Split(prefix)[^1];
                if (after.Length > 1)
                    return after[1..].Split("</div>")[0];
            }
        }
        return string.Empty;
    }

    private static GitHubAsset[] ExtractAssets(string html)
    {
        var assets = new List<GitHubAsset>();
        bool inLi = false;
        string? downloadLink = null;
        string? sha256 = null;
        string? assetId = null;

        foreach (var line in html.Split('\n'))
        {
            if (line.Contains("<li"))
            {
                inLi = true;
                downloadLink = null;
                sha256 = null;
                assetId = null;
            }
            else if (inLi && line.Contains("</li"))
            {
                if (downloadLink is not null && assetId is not null)
                {
                    assets.Add(new GitHubAsset(
                        ProductName: ExtractAssetName(downloadLink.Split('/')[^1]),
                        Id: int.TryParse(assetId, out var id) ? id : 0,
                        Url: "https://github.com" + downloadLink,
                        Sha256: sha256
                    ));
                }
                inLi = false;
            }

            if (!inLi) continue;

            if (downloadLink is null && line.Contains("<a") && line.Contains("href=\"") && line.Contains("/releases/download"))
            {
                var start = line.IndexOf("href=\"", StringComparison.Ordinal) + 6;
                var end = line.IndexOf('"', start);
                if (end > start)
                {
                    var link = line[start..end];
                    // Skip DEBUG builds (unless it's itlwm)
                    var upper = link.ToUpperInvariant();
                    if (upper.Contains("DEBUG") && !link.ToLowerInvariant().Contains("itlwm"))
                    { inLi = false; continue; }
                    downloadLink = link;
                }
            }

            if (sha256 is null && line.Contains("sha256:"))
            {
                sha256 = line.Split("sha256:")[1].Split('<')[0].Trim();
            }

            if (assetId is null && line.Contains("<relative-time"))
            {
                assetId = GenerateAssetId(line);
            }
        }

        return [.. assets];
    }

    private static string GenerateAssetId(string line)
    {
        try
        {
            var dt = line.Split("datetime=\"")[1].Split('"')[0];
            var reversed = new string(dt.Reverse().Where(char.IsDigit).Take(9).ToArray());
            return reversed;
        }
        catch
        {
            return Random.Shared.Next(100_000_000, 999_999_999).ToString();
        }
    }

    public static string ExtractAssetName(string fileName)
    {
        var endIdx = fileName.Length;
        if (fileName.Contains('-')) endIdx = Math.Min(fileName.IndexOf('-'), endIdx);
        if (fileName.Contains('_')) endIdx = Math.Min(fileName.IndexOf('_'), endIdx);
        if (fileName.Contains('.'))
        {
            var dotIdx = fileName.IndexOf('.');
            if (dotIdx > 0 && char.IsDigit(fileName[dotIdx - 1]))
                endIdx = Math.Min(dotIdx - 1, endIdx);
            else
                endIdx = Math.Min(dotIdx, endIdx);
        }

        var name = fileName[..endIdx];

        if (fileName.Contains("Sniffer")) name = fileName.Split('.')[0];
        if (name == "IntelBluetooth") name = "IntelBluetoothFirmware";
        if (fileName.Contains("unsupported")) name += "-unsupported";
        else if (fileName.Contains("rtsx")) name += "-rtsx";
        else if (fileName.ToLowerInvariant().Contains("itlwm"))
        {
            name += fileName switch
            {
                _ when fileName.Contains("Sonoma14.4") => "23.4",
                _ when fileName.Contains("Sonoma14.0") => "23.0",
                _ when fileName.Contains("Ventura")    => "22",
                _ when fileName.Contains("Monterey")   => "21",
                _ when fileName.Contains("BigSur")     => "20",
                _ when fileName.Contains("Catalina")   => "19",
                _ when fileName.Contains("Mojave")     => "18",
                _ when fileName.Contains("HighSierra") => "17",
                _ => string.Empty
            };
        }

        return name;
    }

    /// <summary>
    /// Gets list of releases (currently returns only latest as single-item list for simplicity).
    /// </summary>
    public async Task<List<GitHubReleaseInfo>> GetReleasesAsync(string owner, string repo, CancellationToken ct = default)
    {
        var url = $"https://github.com/{owner}/{repo}/releases";
        var html = await download.FetchTextAsync(url, ct);
        if (html is null)
        {
            logger.LogError("Failed to fetch releases for {Owner}/{Repo}", owner, repo);
            return [];
        }

        var tagName = ExtractTagName(html);
        if (tagName is null)
        {
            logger.LogWarning("No release tag found for {Owner}/{Repo}", owner, repo);
            return [];
        }

        var assetsUrl = $"https://github.com/{owner}/{repo}/releases/expanded_assets/{tagName}";
        var assetsHtml = await download.FetchTextAsync(assetsUrl, ct);
        if (assetsHtml is null)
        {
            logger.LogError("Failed to fetch expanded assets for {Owner}/{Repo}", owner, repo);
            return [];
        }

        var assets = ExtractAssets(assetsHtml);
        return [new GitHubReleaseInfo(tagName, assets)];
    }
}
