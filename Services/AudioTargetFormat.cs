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

    /// <summary>Upper limit the encoder accepts; 0 means no limit. WMA is capped at 48 kHz stereo.</summary>
    public int MaxSampleRate { get; init; }

    /// <summary>Upper channel limit the encoder accepts; 0 means no limit.</summary>
    public int MaxChannels { get; init; }

    /// <summary>Upper bitrate limit the encoder accepts, in kbps; 0 means no limit.</summary>
    public int MaxBitrateKbps { get; init; }
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
        new AudioTargetFormat("WMA",  ".wma",  "wma",    HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 192, DefaultQuality: 5, DefaultBitDepth: 16)
            { MaxSampleRate = 48000, MaxChannels = 2 },

        new AudioTargetFormat("MP2",   ".mp2",   "mp2",     HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 192, DefaultQuality: 5, DefaultBitDepth: 16)
            { MaxSampleRate = 48000, MaxChannels = 2, MaxBitrateKbps = 384 },
        new AudioTargetFormat("AC-3",  ".ac3",   "ac3",     HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 448, DefaultQuality: 5, DefaultBitDepth: 16)
            { MaxSampleRate = 48000, MaxChannels = 6, MaxBitrateKbps = 640 },
        new AudioTargetFormat("E-AC-3", ".eac3", "eac3",    HasBitrate: true,  HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 448, DefaultQuality: 5, DefaultBitDepth: 16)
            { MaxSampleRate = 48000, MaxChannels = 6, MaxBitrateKbps = 6144 },
        new AudioTargetFormat("AMR",   ".amr",   "amr",     HasBitrate: false, HasQuality: false, HasBitDepth: false, HasSampleRate: false, SupportsCoverArt: false, IsLossless: false, DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16)
            { MaxSampleRate = 8000, MaxChannels = 1 },

        new AudioTargetFormat("WavPack", ".wv",  "wavpack", HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: false, IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("TTA",   ".tta",   "tta",     HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: false, IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("CAF",   ".caf",   "pcm",     HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: false, IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
        new AudioTargetFormat("AU",    ".au",    "pcm",     HasBitrate: false, HasQuality: false, HasBitDepth: true,  HasSampleRate: true,  SupportsCoverArt: false, IsLossless: true,  DefaultBitrateKbps: 0,   DefaultQuality: 5, DefaultBitDepth: 16),
    };
}
