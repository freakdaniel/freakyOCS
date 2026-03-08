using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Claunia.PropertyList;

namespace OcsNet.Core.Services;

public sealed class AppUtils
{
    private const string TempPrefix = "ocs_";

    public void CleanTemporaryDirs()
    {
        var tempDir = Path.GetTempPath();
        foreach (var dir in Directory.EnumerateDirectories(tempDir, $"{TempPrefix}*"))
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* ignore */ }
        }
    }

    public string GetTemporaryDir()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{TempPrefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public void WriteFile(string filePath, object data)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        switch (ext)
        {
            case ".json":
                File.WriteAllText(filePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                break;
            case ".plist":
                var plistData = data switch
                {
                    NSDictionary dict => dict,
                    _ => throw new ArgumentException("Plist data must be NSDictionary")
                };
                File.WriteAllBytes(filePath, BinaryPropertyListWriter.WriteToArray(plistData));
                break;
            default:
                if (data is byte[] bytes)
                    File.WriteAllBytes(filePath, bytes);
                else if (data is string str)
                    File.WriteAllText(filePath, str);
                else
                    throw new ArgumentException($"Unsupported data type for extension {ext}");
                break;
        }
    }

    public object? ReadFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".json" => JsonSerializer.Deserialize<JsonDocument>(File.ReadAllText(filePath)),
            ".plist" => PropertyListParser.Parse(filePath),
            _ => File.ReadAllBytes(filePath)
        };
    }

    public T? ReadJson<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath));
    }

    public NSDictionary? ReadPlist(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        return (NSDictionary)PropertyListParser.Parse(filePath);
    }

    public async Task<T?> ReadPlistAsync<T>(string filePath, CancellationToken ct = default) where T : NSObject
    {
        if (!File.Exists(filePath)) return default;
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return PropertyListParser.Parse(bytes) as T;
    }

    public List<(string RelativePath, string Type)> FindMatchingPaths(
        string rootPath,
        string? extensionFilter = null,
        string? nameFilter = null,
        string? typeFilter = null)
    {
        var results = new List<(string, string)>();
        if (!Directory.Exists(rootPath)) return results;

        bool IsValid(string name)
        {
            if (name.StartsWith('.')) return false;
            if (extensionFilter is not null && !name.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase)) return false;
            if (nameFilter is not null && !name.Contains(nameFilter)) return false;
            return true;
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(rootPath, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(entry);
            var relativePath = Path.GetRelativePath(rootPath, entry).Replace('\\', '/');
            var isDir = Directory.Exists(entry);

            if (!IsValid(name)) continue;

            if (typeFilter is null || (typeFilter == "dir" && isDir) || (typeFilter == "file" && !isDir))
                results.Add((relativePath, isDir ? "dir" : "file"));
        }

        return results.OrderBy(x => x.Item1).ToList();
    }

    public void CreateFolder(string path, bool removeContent = false)
    {
        if (Directory.Exists(path) && removeContent)
            Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
    }

    public byte[] HexToBytes(string hexString)
    {
        var cleaned = Regex.Replace(hexString, @"[^0-9a-fA-F]", "");
        if (cleaned.Length % 2 != 0)
            throw new ArgumentException("Invalid hex string");
        return Convert.FromHexString(cleaned);
    }

    public string IntToHex(int number) => number.ToString("X2");

    public string ToLittleEndianHex(string hexString)
    {
        hexString = hexString.TrimStart('0', 'x', 'X').ToLowerInvariant();
        if (hexString.Length % 2 != 0) hexString = "0" + hexString;
        var bytes = Enumerable.Range(0, hexString.Length / 2)
            .Select(i => hexString.Substring(i * 2, 2))
            .Reverse();
        return string.Concat(bytes).ToUpperInvariant();
    }

    public string StringToHex(string str) => Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(str));

    public void ExtractZipFile(string zipPath, string? extractionDirectory = null)
    {
        extractionDirectory ??= Path.ChangeExtension(zipPath, null);
        Directory.CreateDirectory(extractionDirectory);
        ZipFile.ExtractToDirectory(zipPath, extractionDirectory, overwriteFiles: true);
    }

    public string? ContainsAny(IEnumerable<string> data, string searchItem)
    {
        return data.FirstOrDefault(item => searchItem.Contains(item, StringComparison.OrdinalIgnoreCase));
    }

    public string NormalizePath(string path)
    {
        // Remove surrounding quotes
        path = path.Trim().Trim('"', '\'');

        // Expand ~ on Unix
        if (path.StartsWith('~') && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[1..].TrimStart('/'));
        }

        // Normalize separators
        path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        return Path.GetFullPath(path);
    }

    public (int Major, int Minor, int Patch) ParseDarwinVersion(string darwinVersion)
    {
        var parts = darwinVersion.Split('.');
        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    public void OpenFolder(string folderPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start("explorer.exe", folderPath);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", folderPath);
        else
            Process.Start("xdg-open", folderPath);
    }
}
