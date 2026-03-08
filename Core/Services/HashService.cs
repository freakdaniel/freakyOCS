using System.Security.Cryptography;
using System.Text.Json;

namespace OcsNet.Core.Services;

public sealed class HashService
{
    public string? GetSha256(string filePath)
    {
        if (!File.Exists(filePath) || Directory.Exists(filePath))
            return null;

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public Dictionary<string, string>? GenerateFolderManifest(string folderPath, string? manifestPath = null)
    {
        if (!Directory.Exists(folderPath))
            return null;

        manifestPath ??= Path.Combine(folderPath, "manifest.json");
        var manifestFileName = Path.GetFileName(manifestPath);

        var manifest = new Dictionary<string, string>();
        foreach (var filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(folderPath, filePath).Replace('\\', '/');
            if (relativePath == manifestFileName) continue;

            var hash = GetSha256(filePath);
            if (hash is not null)
                manifest[relativePath] = hash;
        }

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return manifest;
    }

    public (bool? IsValid, FolderIntegrityIssues Issues) VerifyFolderIntegrity(string folderPath, string? manifestPath = null)
    {
        if (!Directory.Exists(folderPath))
            return (null, new FolderIntegrityIssues("Folder not found."));

        manifestPath ??= Path.Combine(folderPath, "manifest.json");

        if (!File.Exists(manifestPath))
            return (null, new FolderIntegrityIssues("Manifest file not found."));

        Dictionary<string, string>? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(manifestPath));
        }
        catch
        {
            return (null, new FolderIntegrityIssues("Invalid manifest file."));
        }

        if (manifest is null)
            return (null, new FolderIntegrityIssues("Invalid manifest file."));

        var manifestFileName = Path.GetFileName(manifestPath);
        var issues = new FolderIntegrityIssues();
        var actualFiles = new HashSet<string>();

        foreach (var filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(folderPath, filePath).Replace('\\', '/');
            if (relativePath == manifestFileName) continue;

            actualFiles.Add(relativePath);

            if (!manifest.ContainsKey(relativePath))
                issues.Untracked.Add(relativePath);
            else if (GetSha256(filePath) != manifest[relativePath])
                issues.Modified.Add(relativePath);
        }

        foreach (var key in manifest.Keys.Where(k => !actualFiles.Contains(k)))
            issues.Missing.Add(key);

        return (!issues.HasIssues, issues);
    }
}

public sealed class FolderIntegrityIssues(string? errorMessage = null)
{
    public List<string> Modified  { get; } = [];
    public List<string> Missing   { get; } = [];
    public List<string> Untracked { get; } = [];
    public string? ErrorMessage   { get; } = errorMessage;
    public bool HasIssues => Modified.Count > 0 || Missing.Count > 0 || Untracked.Count > 0;
}
