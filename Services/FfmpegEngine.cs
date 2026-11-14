using System.Diagnostics;
using System.Text;

namespace AudioConverter.Services;

/// <summary>Converts audio by running the bundled ffmpeg.exe as a child process.</summary>
public sealed class FfmpegEngine : IAudioEngine
{
    readonly string _ffmpegPath;

    public FfmpegEngine(string? ffmpegPath = null) => _ffmpegPath = ffmpegPath ?? FfmpegLocator.Resolve();

    public string FfmpegPath => _ffmpegPath;

    public void Convert(string inputPath, string outputPath, AudioConvertOptions options, CancellationToken ct = default)
    {
        // Write under a temporary name that still ends in the real extension, so ffmpeg picks the right muxer
        // and a failure never leaves a half-written file behind.
        string temp = outputPath + ".part" + options.Target.Extension;
        try
        {
            bool includeCoverArt = options.KeepMetadata && options.Target.SupportsCoverArt;
            try
            {
                Run(BuildArguments(inputPath, temp, options, includeCoverArt), ct);
            }
            catch (InvalidOperationException) when (includeCoverArt && !ct.IsCancellationRequested)
            {
                // Some inputs carry a picture the target container can't store (an odd art codec, or a WAV with
                // an embedded image). Tags still come across, so retry once without the picture.
                Run(BuildArguments(inputPath, temp, options, includeCoverArt: false), ct);
            }

            File.Move(temp, outputPath, overwrite: false);
        }
        finally
        {
            if (File.Exists(temp))
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }

    /// <summary>The full ffmpeg command line for one file. Public so it can be inspected in tests.</summary>
    public static IReadOnlyList<string> BuildArguments(string input, string output, AudioConvertOptions o, bool includeCoverArt = true)
    {
        var a = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-nostdin", "-y",
            "-i", input,
            "-map", "0:a:0",          // first audio stream only
        };

        if (o.KeepMetadata)
        {
            a.AddRange(new[] { "-map_metadata", "0", "-map_chapters", "0" });
            if (o.Target.SupportsCoverArt && includeCoverArt)
            {
                // Cover art arrives as a video stream; copy it across untouched as an attached picture.
                a.AddRange(new[] { "-map", "0:v?", "-c:v", "copy", "-disposition:v", "attached_pic" });
            }
        }
        else
        {
            a.AddRange(new[] { "-map_metadata", "-1", "-map_chapters", "-1" });
        }

        // Some encoders accept a narrower range than the source (WMA is 48 kHz stereo at most).
        // Clamp instead of failing: refusing a file that could otherwise convert helps nobody.
        int sampleRate = o.SampleRate;
        if (o.Target.MaxSampleRate > 0)
            sampleRate = sampleRate > 0 ? Math.Min(sampleRate, o.Target.MaxSampleRate) : o.Target.MaxSampleRate;

        if (sampleRate > 0)
        {
            a.Add("-ar");
            a.Add(sampleRate.ToString());
        }

        int channels = o.Channels switch
        {
            ChannelMode.Mono => 1,
            ChannelMode.Stereo => 2,
            _ => 0
        };
        if (channels == 0) channels = o.Target.MaxChannels;

        if (channels > 0)
        {
            a.Add("-ac");
            a.Add(channels.ToString());
        }

        int bitrate = o.BitrateKbps;
        if (o.Target.MaxBitrateKbps > 0) bitrate = Math.Min(bitrate, o.Target.MaxBitrateKbps);

        switch (o.Target.Family)
        {
            case "mp3":
                a.Add("-c:a"); a.Add("libmp3lame");
                a.Add("-b:a"); a.Add($"{bitrate}k");
                break;

            case "aac":
                a.Add("-c:a"); a.Add("aac");
                a.Add("-b:a"); a.Add($"{bitrate}k");
                break;

            case "flac":
                a.Add("-c:a"); a.Add("flac");
                a.Add("-compression_level"); a.Add("5");
                AddSampleFormat(a, o.BitDepth, planar: false);
                break;

            case "alac":
                a.Add("-c:a"); a.Add("alac");
                // ALAC only accepts planar formats (s16p / s32p); packed ones are rejected outright.
                AddSampleFormat(a, o.BitDepth, planar: true);
                break;

            case "wavpack":
                // WavPack rejects a frame size under 128 samples, and ffmpeg sizes the encoder's frames from the
                // first decoded frame. Gapless trimming in MP3/AAC can leave that first frame tiny (an MP3 often
                // starts with 47 samples), which fails with "invalid block size". Re-chunking to a fixed block
                // size avoids it; the filter is lossless, so the audio stays sample-for-sample identical.
                a.Add("-af"); a.Add("asetnsamples=n=8192:p=0");
                a.Add("-c:a"); a.Add("wavpack");
                a.Add("-compression_level"); a.Add("5");
                // WavPack is the opposite way round: it only takes planar formats.
                AddSampleFormat(a, o.BitDepth, planar: true);
                break;

            case "tta":
                a.Add("-c:a"); a.Add("tta");
                AddSampleFormat(a, o.BitDepth, planar: false);
                break;

            case "pcm":
                // For PCM the codec itself carries the bit depth (and byte order) so pick it directly.
                bool bigEndian = o.Target.Extension is ".aiff" or ".au";
                a.Add("-c:a");
                a.Add(!bigEndian
                    ? (o.BitDepth >= 24 ? "pcm_s24le" : "pcm_s16le")
                    : (o.BitDepth >= 24 ? "pcm_s24be" : "pcm_s16be"));
                break;

            case "vorbis":
                a.Add("-c:a"); a.Add("libvorbis");
                a.Add("-q:a"); a.Add(o.Quality.ToString());
                break;

            case "opus":
                a.Add("-c:a"); a.Add("libopus");
                a.Add("-b:a"); a.Add($"{bitrate}k");
                break;

            case "wma":
                a.Add("-c:a"); a.Add("wmav2");
                a.Add("-b:a"); a.Add($"{bitrate}k");
                break;

            case "mp2":
                a.Add("-c:a"); a.Add("mp2");
                a.Add("-b:a"); a.Add($"{bitrate}k");
                break;

            case "ac3":
                a.Add("-c:a"); a.Add("ac3");
                a.Add("-b:a"); a.Add($"{bitrate}k");
                break;

            case "eac3":
                a.Add("-c:a"); a.Add("eac3");
                a.Add("-b:a"); a.Add($"{bitrate}k");
                break;

            case "amr":
                // AMR-NB has fixed modes; 12.2 kbps is the best quality it offers.
                a.Add("-c:a"); a.Add("libopencore_amrnb");
                a.Add("-b:a"); a.Add("12.2k");
                break;

            default:
                throw new InvalidOperationException("Unsupported target format: " + o.Target.Name);
        }

        a.Add(output);
        return a;
    }

    /// <summary>
    /// FLAC, ALAC and WavPack take a sample format; s32 is how ffmpeg writes 24-bit. ALAC and WavPack
    /// require the planar variants (s16p / s32p) - the packed ones are rejected by those encoders.
    /// </summary>
    static void AddSampleFormat(List<string> a, int bitDepth, bool planar)
    {
        string format = bitDepth >= 24 ? "s32" : "s16";
        a.Add("-sample_fmt");
        a.Add(planar ? format + "p" : format);
    }

    void Run(IReadOnlyList<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(_ffmpegPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        foreach (string arg in args) psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        var errors = new StringBuilder();

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            lock (errors)
            {
                if (errors.Length < 4000) errors.AppendLine(e.Data);
            }
        };

        if (!process.Start()) throw new InvalidOperationException("ffmpeg could not be started.");
        process.BeginErrorReadLine();

        using var registration = ct.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        process.WaitForExit();
        ct.ThrowIfCancellationRequested();

        if (process.ExitCode != 0)
        {
            string detail = errors.ToString().Trim();
            var lines = detail.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string message = lines.Length > 0 ? lines[^1].Trim() : detail;
            throw new InvalidOperationException(message.Length > 0 ? message : $"ffmpeg failed (exit code {process.ExitCode}).");
        }
    }
}
