using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ISImage = SixLabors.ImageSharp.Image;

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
            Arguments = $"-nostdin -y -i \"{inputFile}\" \"{outputPattern}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"FFmpeg a echoue.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    public async Task ExportMpegAsync(
        IReadOnlyList<string> framePaths,
        string outputFile,
        int fps,
        string? workDirectory = null,
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
        string? logFilePath = PrepareFfmpegLogPath(workDirectory, "ffmpeg_export_mp4");

        try
        {
            for (int i = 0; i < framePaths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string destination = Path.Combine(sequenceDir, $"frame_{i:0000}.png");
                File.Copy(framePaths[i], destination, true);
            }

            string fpsText = Math.Max(1, fps).ToString(CultureInfo.InvariantCulture);
            string inputPattern = Path.Combine(sequenceDir, "frame_%04d.png");
            string videoFilter = BuildEvenDimensionsVideoFilter(framePaths[0]);
            string arguments =
                $"-nostdin -y -framerate {fpsText} -i \"{inputPattern}\" {videoFilter}-c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -movflags +faststart \"{outputFile}\"";

            var psi = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            string stdout = await stdoutTask;
            string stderr = await stderrTask;
            WriteFfmpegLog(logFilePath, psi.FileName, psi.Arguments, process.ExitCode, stdout, stderr);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"FFmpeg a echoue.{Environment.NewLine}Journal : {logFilePath ?? "(non disponible)"}{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
            }
        }
        finally
        {
            if (Directory.Exists(sequenceDir))
                Directory.Delete(sequenceDir, true);
        }
    }

    private static string BuildEvenDimensionsVideoFilter(string firstFramePath)
    {
        var info = ISImage.Identify(firstFramePath);
        if (info is null)
            return string.Empty;

        int targetWidth = info.Width - (info.Width % 2);
        int targetHeight = info.Height - (info.Height % 2);

        if (targetWidth <= 0 || targetHeight <= 0)
            throw new InvalidOperationException($"Dimensions invalides pour l'export video : {info.Width}x{info.Height}.");

        if (targetWidth == info.Width && targetHeight == info.Height)
            return string.Empty;

        return $"-vf \"crop={targetWidth}:{targetHeight}:0:0\" ";
    }

    private static string? PrepareFfmpegLogPath(string? workDirectory, string operationName)
    {
        if (string.IsNullOrWhiteSpace(workDirectory))
            return null;

        Directory.CreateDirectory(workDirectory);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(workDirectory, $"{operationName}_{timestamp}.log");
    }

    private static void WriteFfmpegLog(
        string? logFilePath,
        string executablePath,
        string arguments,
        int exitCode,
        string stdout,
        string stderr)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
            return;

        string content =
            $"Timestamp: {DateTime.Now:O}{Environment.NewLine}" +
            $"Executable: {executablePath}{Environment.NewLine}" +
            $"Arguments: {arguments}{Environment.NewLine}" +
            $"ExitCode: {exitCode}{Environment.NewLine}" +
            $"{Environment.NewLine}--- STDOUT ---{Environment.NewLine}{stdout}" +
            $"{Environment.NewLine}--- STDERR ---{Environment.NewLine}{stderr}";

        File.WriteAllText(logFilePath, content);
    }
}
