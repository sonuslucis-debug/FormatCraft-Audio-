namespace AudioConverter.Services;

/// <summary>Reads one audio file and writes it in the target format. Kept behind an interface so the batch logic can be tested.</summary>
public interface IAudioEngine
{
    void Convert(string inputPath, string outputPath, AudioConvertOptions options, CancellationToken ct = default);
}
