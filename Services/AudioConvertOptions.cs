namespace AudioConverter.Services;

public enum ChannelMode { Keep, Mono, Stereo }

/// <param name="Target">The output format.</param>
/// <param name="BitrateKbps">Used by lossy formats that take a bitrate (MP3, AAC, Opus, WMA).</param>
/// <param name="Quality">0 to 10, used by Ogg Vorbis (variable bitrate).</param>
/// <param name="SampleRate">0 keeps the source rate.</param>
/// <param name="BitDepth">16 or 24, used by the lossless targets.</param>
/// <param name="Channels">Keep the source layout, or force mono or stereo.</param>
/// <param name="KeepMetadata">Carry tags, cover art and chapters across.</param>
public sealed record AudioConvertOptions(
    AudioTargetFormat Target,
    int BitrateKbps,
    int Quality,
    int SampleRate,
    int BitDepth,
    ChannelMode Channels,
    bool KeepMetadata);
