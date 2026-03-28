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
                $"ffmpeg.exe was not found. Copy ffmpeg.exe next to the application executable. Expected path: {FfmpegPath}");
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
                $"FFmpeg failed.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    public async Task ExportMpegAsync(
        IReadOnlyList<string> framePaths,
        string outputFile,
        int fps,
        string? workDirectory = null,
        CancellationToken cancellationToken = default)
    {
        string fpsText = Math.Max(1, fps).ToString(CultureInfo.InvariantCulture);
        await ExportVideoFromFramesAsync(
            framePaths,
            outputFile,
            fpsText,
            workDirectory,
            "ffmpeg_export_mp4",
            true,
            (inputPattern, outputPath, filter) =>
                $"-nostdin -y -framerate {fpsText} -i \"{inputPattern}\" {filter}-c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -movflags +faststart \"{outputPath}\"",
            cancellationToken);
    }

    public async Task ExportWebMAsync(
        IReadOnlyList<string> framePaths,
        string outputFile,
        int fps,
        string? workDirectory = null,
        CancellationToken cancellationToken = default)
    {
        string fpsText = Math.Max(1, fps).ToString(CultureInfo.InvariantCulture);
        await ExportVideoFromFramesAsync(
            framePaths,
            outputFile,
            fpsText,
            workDirectory,
            "ffmpeg_export_webm",
            true,
            (inputPattern, outputPath, filter) =>
                $"-nostdin -y -framerate {fpsText} -i \"{inputPattern}\" {filter}-c:v libvpx-vp9 -b:v 0 -crf 32 -pix_fmt yuv420p -row-mt 1 \"{outputPath}\"",
            cancellationToken);
    }

    public async Task ExportAnimatedWebpAsync(
        IReadOnlyList<string> framePaths,
        string outputFile,
        int fps,
        string? workDirectory = null,
        CancellationToken cancellationToken = default)
    {
        string fpsText = Math.Max(1, fps).ToString(CultureInfo.InvariantCulture);
        await ExportVideoFromFramesAsync(
            framePaths,
            outputFile,
            fpsText,
            workDirectory,
            "ffmpeg_export_webp",
            false,
            (inputPattern, outputPath, filter) =>
                $"-nostdin -y -framerate {fpsText} -i \"{inputPattern}\" {filter}-c:v libwebp_anim -loop 0 -quality 80 -compression_level 4 -pix_fmt yuva420p \"{outputPath}\"",
            cancellationToken);
    }

    private async Task ExportVideoFromFramesAsync(
        IReadOnlyList<string> framePaths,
        string outputFile,
        string fpsText,
        string? workDirectory,
        string logOperationName,
        bool needsEvenDimensions,
        Func<string, string, string, string> buildArguments,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable())
        {
            throw new FileNotFoundException(
                $"ffmpeg.exe was not found. Copy ffmpeg.exe next to the application executable. Expected path: {FfmpegPath}");
        }

        if (framePaths.Count == 0)
            throw new InvalidOperationException("No images to export.");

        string sequenceDir = Path.Combine(Path.GetTempPath(), $"MotionPhotoWorkbench_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sequenceDir);
        string? logFilePath = PrepareFfmpegLogPath(workDirectory, logOperationName);

        try
        {
            for (int i = 0; i < framePaths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string destination = Path.Combine(sequenceDir, $"frame_{i:0000}.png");
                File.Copy(framePaths[i], destination, true);
            }

            string inputPattern = Path.Combine(sequenceDir, "frame_%04d.png");
            string videoFilter = needsEvenDimensions
                ? BuildEvenDimensionsVideoFilter(framePaths[0])
                : string.Empty;
            string arguments = buildArguments(inputPattern, outputFile, videoFilter);

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
                $"FFmpeg failed.{Environment.NewLine}Log: {logFilePath ?? "(not available)"}{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
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
            throw new InvalidOperationException($"Invalid dimensions for video export: {info.Width}x{info.Height}.");

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
