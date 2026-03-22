using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
                $"ffmpeg.exe est introuvable. Copie ffmpeg.exe a cote de l'executable de l'application. Chemin attendu : {FfmpegPath}");
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (string existing in Directory.GetFiles(outputDirectory, "frame_*.png"))
            File.Delete(existing);

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
                $"FFmpeg a echoue.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    public async Task ExportMpegAsync(
        IReadOnlyList<string> framePaths,
        string outputFile,
        int delayCs,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            throw new FileNotFoundException(
                $"ffmpeg.exe est introuvable. Copie ffmpeg.exe a cote de l'executable de l'application. Chemin attendu : {FfmpegPath}");
        }

        if (framePaths.Count == 0)
            throw new InvalidOperationException("Aucune image a exporter.");

        string sequenceDir = Path.Combine(Path.GetTempPath(), $"MotionPhotoWorkbench_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sequenceDir);

        try
        {
            for (int i = 0; i < framePaths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string destination = Path.Combine(sequenceDir, $"frame_{i:0000}.png");
                File.Copy(framePaths[i], destination, true);
            }

            double fps = Math.Max(1d, 100d / Math.Max(1, delayCs));
            string fpsText = fps.ToString("0.###", CultureInfo.InvariantCulture);
            string inputPattern = Path.Combine(sequenceDir, "frame_%04d.png");

            var psi = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = $"-y -framerate {fpsText} -i \"{inputPattern}\" -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -movflags +faststart \"{outputFile}\"",
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
                    $"FFmpeg a echoue.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
            }
        }
        finally
        {
            if (Directory.Exists(sequenceDir))
                Directory.Delete(sequenceDir, true);
        }
    }
}
