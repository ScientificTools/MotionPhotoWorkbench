using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using System.Diagnostics;

namespace MotionPhotoWorkbench.Services;

public sealed class FfmpegService
{
    public string FfmpegPath { get; }

    public FfmpegService(string ffmpegPath)
    {
        FfmpegPath = ffmpegPath;
    }

    public bool IsAvailable() => File.Exists(FfmpegPath);

    public async Task ExtractFramesAsync(
        string inputFile,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            throw new FileNotFoundException(
                $"ffmpeg.exe est introuvable. Copie ffmpeg.exe à côté de l'exécutable de l'application. Chemin attendu : {FfmpegPath}");
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (string existing in Directory.GetFiles(outputDirectory, "frame_*.png"))
        {
            File.Delete(existing);
        }

        string outputPattern = Path.Combine(outputDirectory, "frame_%04d.png");

        var psi = new ProcessStartInfo
        {
            FileName = FfmpegPath,
            Arguments = $"-y -i \"{inputFile}\" \"{outputPattern}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"FFmpeg a échoué.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }
}
