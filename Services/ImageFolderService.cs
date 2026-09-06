using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace MotionPhotoWorkbench.Services;

public sealed class ImageFolderService
{
    public static string GetWorkingDirectory(string folder) => Path.Combine(Path.GetFullPath(folder), "_work");

    public static List<string> GetImageNames(string folder) => Directory.EnumerateFiles(folder)
        .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp")
        .Select(path => Path.GetFileName(path))
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(name => name, StringComparer.Ordinal)
        .ToList();

    public static void ValidateWorkingDirectory(string folder, string workDirectory)
    {
        if (!string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(workDirectory)),
                GetWorkingDirectory(folder), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The image folder cache must be the source folder's _work subdirectory.");

        foreach (string path in new[] { workDirectory, Path.Combine(workDirectory, "frames") })
        {
            if (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException($"The cache cannot use a linked directory: {path}");
        }
    }

    public static async Task<List<string>> ImportAsync(string folder, string workDirectory, IReadOnlyList<string> names)
    {
        ValidateWorkingDirectory(folder, workDirectory);
        if (names.Count == 0)
            throw new InvalidOperationException("No PNG, JPG, JPEG, GIF or BMP images were found in the selected folder.");

        foreach (string name in names)
        {
            if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || !File.Exists(Path.Combine(folder, name)))
                throw new FileNotFoundException($"Source image missing or invalid: {name}");
        }

        string framesDirectory = Path.Combine(workDirectory, "frames");
        Directory.CreateDirectory(framesDirectory);
        var frames = new List<string>();
        for (int i = 0; i < names.Count; i++)
        {
            string destination = Path.Combine(framesDirectory, $"frame_{(i + 1).ToString("D3", CultureInfo.InvariantCulture)}.png");
            if (File.Exists(destination) && (File.GetAttributes(destination) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException($"The cache cannot overwrite a linked file: {destination}");
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(Path.Combine(folder, names[i]));
            // One output per source file, including animated GIFs.
            using var firstFrame = image.Frames.CloneFrame(0);
            // Unlink an existing cache file first so a hard link cannot modify a source image.
            if (File.Exists(destination))
                File.Delete(destination);
            await firstFrame.SaveAsync(destination, new PngEncoder());
            frames.Add(destination);
        }
        var currentFrames = frames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string oldFrame in Directory.EnumerateFiles(framesDirectory, "frame_*.png"))
        {
            if (!currentFrames.Contains(oldFrame))
                File.Delete(oldFrame);
        }
        return frames;
    }
}
