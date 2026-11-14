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
            Run(BuildArguments(inputPath, temp, options), ct);
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
    public static IReadOnlyList<string> BuildArguments(string input, string output, AudioConvertOptions o)
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
            if (o.Target.SupportsCoverArt)
            {
                // Cover art arrives as a video stream; copy it across untouched as an attached picture.
                a.AddRange(new[] { "-map", "0:v?", "-c:v", "copy", "-disposition:v", "attached_pic" });
            }
        }
        else
        {
            a.AddRange(new[] { "-map_metadata", "-1", "-map_chapters", "-1" });
        }

        if (o.SampleRate > 0)
        {
            a.Add("-ar");
            a.Add(o.SampleRate.ToString());
        }

        if (o.Channels == ChannelMode.Mono)
        {
            a.Add("-ac");
            a.Add("1");
        }
        else if (o.Channels == ChannelMode.Stereo)
        {
            a.Add("-ac");
            a.Add("2");
        }

        switch (o.Target.Family)
        {
            case "mp3":
                a.Add("-c:a"); a.Add("libmp3lame");
                a.Add("-b:a"); a.Add($"{o.BitrateKbps}k");
                break;

            case "aac":
                a.Add("-c:a"); a.Add("aac");
                a.Add("-b:a"); a.Add($"{o.BitrateKbps}k");
                break;

            case "flac":
                a.Add("-c:a"); a.Add("flac");
                a.Add("-compression_level"); a.Add("5");
                AddSampleFormat(a, o.BitDepth);
                break;

            case "alac":
                a.Add("-c:a"); a.Add("alac");
                AddSampleFormat(a, o.BitDepth);
                break;

            case "pcm":
                // For PCM the codec itself carries the bit depth, so pick it directly.
                bool bigEndian = o.Target.Extension == ".aiff";
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
                a.Add("-b:a"); a.Add($"{o.BitrateKbps}k");
                break;

            case "wma":
                a.Add("-c:a"); a.Add("wmav2");
                a.Add("-b:a"); a.Add($"{o.BitrateKbps}k");
                break;

            default:
                throw new InvalidOperationException("Unsupported target format: " + o.Target.Name);
        }

        a.Add(output);
        return a;
    }

    /// <summary>FLAC and ALAC take a sample format; s32 is how ffmpeg writes 24-bit.</summary>
    static void AddSampleFormat(List<string> a, int bitDepth)
    {
        a.Add("-sample_fmt");
        a.Add(bitDepth >= 24 ? "s32" : "s16");
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
