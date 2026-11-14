namespace AudioConverter.Services;

/// <summary>An output format the user can pick, with the options that make sense for it.</summary>
/// <param name="Family">Which ffmpeg codec family to use; see <see cref="FfmpegEngine"/>.</param>
public sealed record AudioTargetFormat(
    string Name,
    string Extension,
    string Family,
    bool HasBitrate,
    bool HasQuality,
    bool HasBitDepth,
    bool HasSampleRate,
    bool SupportsCoverArt,
    bool IsLossless,
    int DefaultBitrateKbps,
    int DefaultQuality,
    int DefaultBitDepth)
{
    public override string ToString() => Name;
}

public static class AudioTargetFormats
{
    /// <summary>Every format the app offers.</summary>
    public static readonly IReadOnlyList<AudioTargetFormat> All = new[]
    {
        new AudioTargetFormat("MP3",  ".mp3",  "mp3",    HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: true,  IsLossless: false, DefaultBitrateKbps: 192, DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("AAC",  ".m4a",  "aac",    HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: true,  IsLossless: false, DefaultBitrateKbps: 192, DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("FLAC", ".flac", "flac",   HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: true,  IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("ALAC", ".m4a",  "alac",   HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: true,  IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("WAV",  ".wav",  "pcm",    HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: false, IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("AIFF", ".aiff", "pcm",    HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: false, IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("OGG",  ".ogg",  "vorbis", HasBitrate: false, HasQuality: true,  HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 0,   DefaultQuality: 6, DefaultBitDepth: 16),
        new AudioTargetFormat("Opus", ".opus", "opus",   HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 128, DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("WMA",  ".wma",  "wma",    HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 192, DefaultQuality: 5, DefaultBitDepth: 16),
    };
}
