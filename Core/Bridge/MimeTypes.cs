namespace OcsNet.Core.Bridge;

internal static class MimeTypes
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html",
        [".htm"]  = "text/html",
        [".css"]  = "text/css",
        [".js"]   = "application/javascript",
        [".mjs"]  = "application/javascript",
        [".ts"]   = "application/typescript",
        [".json"] = "application/json",
        [".svg"]  = "image/svg+xml",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"]  = "image/gif",
        [".ico"]  = "image/x-icon",
        [".webp"] = "image/webp",
        [".woff"] = "font/woff",
        [".woff2"]= "font/woff2",
        [".ttf"]  = "font/ttf",
        [".otf"]  = "font/otf",
        [".map"]  = "application/json",
        [".txt"]  = "text/plain",
    };

    public static string Get(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return _map.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }
}
