namespace AudioConverter.Services;

/// <summary>Friendly names for the input formats shown on the drop box.</summary>
public static class SupportedFormats
{
    /// <summary>Shown if the list can't be built for some reason.</summary>
    public const string Fallback =
        "MP3 \u00B7 WAV \u00B7 FLAC \u00B7 AIFF \u00B7 M4A/AAC \u00B7 OGG \u00B7 Opus \u00B7 WMA \u00B7 and more";

    static readonly (string Label, string[] Extensions)[] Known =
    {
        ("MP3", new[] { ".mp3" }),
        ("WAV", new[] { ".wav", ".w64", ".rf64" }),
        ("FLAC", new[] { ".flac" }),
        ("AIFF", new[] { ".aiff", ".aif", ".aifc" }),
        ("M4A/AAC", new[] { ".m4a", ".m4b", ".aac" }),
        ("OGG", new[] { ".ogg", ".oga" }),
        ("Opus", new[] { ".opus" }),
        ("WMA", new[] { ".wma" }),
        ("WavPack", new[] { ".wv" }),
        ("APE", new[] { ".ape" }),
        ("CAF", new[] { ".caf" }),
        ("AMR", new[] { ".amr" }),
        ("AU", new[] { ".au" }),
        ("Matroska", new[] { ".mka" }),
        ("DSD", new[] { ".dsf", ".dff" }),
    };

    public static string Describe() => string.Join(" \u00B7 ", Known.Select(k => k.Label));
}
