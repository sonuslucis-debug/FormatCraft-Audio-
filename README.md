# FormatCraft [Audio]

A fast, standalone Windows program for converting audio files from one format to another. Drop in files or whole folders, pick a target format, and convert them all at once.

## Features

- **Drag and drop.** Drop files or folders onto the window, or use **Add files** and **Add folder**. Folders can include their subfolders.
- **Reads almost any audio format.** MP3, WAV, FLAC, AIFF, M4A/AAC, OGG, Opus, WMA, WavPack, APE, CAF, AMR, AU, MKA, DSD and more.
- **Converts to** MP3, AAC (M4A), FLAC, ALAC (M4A), WAV, AIFF, OGG Vorbis, Opus and WMA.
- **Correct options per format.** Bitrate for lossy formats, bit depth and sample rate for lossless ones, a quality slider for OGG Vorbis, and optional mono/stereo.
- **Tags stay put.** Artist, album and track data, cover art and chapters are carried across where the target format supports them.
- **Honest about quality.** Converting a lossy file to a lossless format makes it bigger, not better — the app says so before you start.
- **Fast.** Several files are converted at once, with live progress and speed.
- **Safe.** Originals are never touched or overwritten. Name clashes get a number: `track.flac`, `track (2).flac`.
- **Your choice of output.** Save next to the originals, or into a folder you choose (sub-folder structure is kept).

## Notes

- This tool is for audio only. Images, documents and video files are skipped.
- Converting from a lossy format (MP3, AAC, OGG, Opus, WMA) to a lossless one (FLAC, ALAC, WAV, AIFF) cannot restore what was already thrown away.
- Everything happens on your own computer. No audio is uploaded anywhere.

## Requirements

Windows 10 or 11, 64-bit.

## Building from source

The project builds automatically on GitHub: upload it to a repository and the workflow in `.github/workflows/build.yml` produces `FormatCraftAudio.exe` under **Actions**. The workflow downloads a static ffmpeg build and embeds it in the exe, so the result is a single file.

To build it yourself, install the .NET 8 SDK, put a static `ffmpeg.exe` in a `Tools` folder next to the project file, then run:

```
dotnet publish AudioConverter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Without a `Tools\ffmpeg.exe`, the app still builds but expects ffmpeg on the PATH at runtime.

## Credits

- **Created by Sonus Lucis**: concept, design, direction, logos and artwork, and testing.
- **Programmed by Claude**, made by Anthropic: all of the code.
- **Audio processing:** [FFmpeg](https://ffmpeg.org) — see `THIRD-PARTY-NOTICES.txt`.
- **Typeface:** Montserrat, by The Montserrat Project Authors, under the SIL Open Font License 1.1 (see `Fonts/OFL.txt`).

[sonuslucis.com](https://sonuslucis.com)
