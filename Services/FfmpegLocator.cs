using System.Reflection;
using System.Security.Cryptography;

namespace AudioConverter.Services;

/// <summary>
/// Finds ffmpeg.exe. Order: a copy next to the app (handy while developing), then the copy embedded in the
/// published exe (unpacked once into LocalAppData), then whatever is on the PATH.
/// </summary>
public static class FfmpegLocator
{
    public const string ExeName = "ffmpeg.exe";

    static readonly object Gate = new();
    static string? _cached;

    public static string Resolve()
    {
        lock (Gate)
        {
            if (_cached != null && File.Exists(_cached)) return _cached;

            string local = Path.Combine(AppContext.BaseDirectory, "Tools", ExeName);
            if (File.Exists(local)) return _cached = local;

            string beside = Path.Combine(AppContext.BaseDirectory, ExeName);
            if (File.Exists(beside)) return _cached = beside;

            string? extracted = TryExtractEmbedded();
            if (extracted != null) return _cached = extracted;

            string? onPath = FindOnPath();
            if (onPath != null) return _cached = onPath;

            throw new FileNotFoundException(
                "ffmpeg.exe was not found. Put it in a Tools folder next to the app, or install it and add it to PATH.");
        }
    }

    static string? TryExtractEmbedded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string? name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ExeName, StringComparison.OrdinalIgnoreCase));
        if (name == null) return null;

        using Stream? stream = assembly.GetManifestResourceStream(name);
        if (stream == null) return null;

        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FormatCraftAudio", "tools");
        Directory.CreateDirectory(folder);

        string target = Path.Combine(folder, $"ffmpeg-{ShortHash(stream)}.exe");
        if (File.Exists(target) && new FileInfo(target).Length > 0) return target;

        string temp = target + ".tmp";
        try
        {
            using (var file = File.Create(temp)) stream.CopyTo(file);
            File.Move(temp, target, overwrite: true);
            return target;
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            return null;
        }
    }

    /// <summary>Names the unpacked copy after its contents, so an upgraded exe doesn't reuse the old binary.</summary>
    static string ShortHash(Stream stream)
    {
        stream.Position = 0;
        using var sha = SHA256.Create();
        string hex = Convert.ToHexString(sha.ComputeHash(stream));
        stream.Position = 0;
        return hex[..12].ToLowerInvariant();
    }

    static string? FindOnPath()
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path)) return null;

        foreach (string dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                string candidate = Path.Combine(dir.Trim().Trim('"'), ExeName);
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* an invalid PATH entry: ignore it */ }
        }
        return null;
    }
}
