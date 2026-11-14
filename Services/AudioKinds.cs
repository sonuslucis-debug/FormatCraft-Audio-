namespace AudioConverter.Services;

/// <summary>Which files count as audio when a folder is added or files are dropped.</summary>
public static class AudioKinds
{
    /// <summary>Audio formats picked up when scanning folders.</summary>
    public static readonly HashSet<string> Readable = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".aiff", ".aif", ".aifc", ".m4a", ".m4b", ".aac", ".ogg", ".oga",
        ".opus", ".wma", ".wv", ".ape", ".caf", ".amr", ".au", ".mka", ".mp2", ".ac3", ".dts",
        ".tta", ".dsf", ".dff", ".w64", ".rf64"
    };

    /// <summary>Formats that already threw information away. Converting these to a lossless format cannot bring it back.</summary>
    public static readonly HashSet<string> Lossy = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".m4a", ".m4b", ".aac", ".ogg", ".oga", ".opus", ".wma", ".amr", ".mp2", ".ac3"
    };

    /// <summary>Image, video and document formats: this tool is for audio only, so these are skipped.</summary>
    public static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".heif", ".avif", ".tif", ".tiff",
        ".svg", ".pdf", ".eps", ".ai", ".ps", ".doc", ".docx", ".txt", ".rtf", ".md",
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".flv", ".m4v",
        ".zip", ".rar", ".7z"
    };

    public static bool IsAudio(string path) => Readable.Contains(Path.GetExtension(path));

    public static bool IsLossy(string path) => Lossy.Contains(Path.GetExtension(path));

    public static bool IsExcluded(string path) => Excluded.Contains(Path.GetExtension(path));

    /// <summary>A short label for the list, e.g. "FLAC".</summary>
    public static string Label(string path)
    {
        string ext = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        return ext.Length == 0 ? "?" : ext;
    }
}
