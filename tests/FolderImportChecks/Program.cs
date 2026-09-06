using System.Reflection;
using System.Security.Cryptography;
using MotionPhotoWorkbench;
using MotionPhotoWorkbench.Models;
using MotionPhotoWorkbench.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

internal static class Program
{
    [STAThread]
    private static void Main() => RunAsync().GetAwaiter().GetResult();

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static async Task RunAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "MotionPhotoFolderChecks-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        string[] names = { "e.bmp", "B.JPG", "a.png", "d.gif", "c.jpeg" };
        for (int i = 0; i < names.Length; i++)
        {
            using var image = new Image<Rgba32>(2, 2, new Rgba32((byte)(i * 40), 0, 0));
            if (names[i].EndsWith("gif"))
                image.Frames.AddFrame(image.Frames.RootFrame);
            await image.SaveAsync(Path.Combine(root, names[i]));
        }
        await File.WriteAllTextAsync(Path.Combine(root, "ignored.txt"), "untouched");
        Directory.CreateDirectory(Path.Combine(root, "nested"));
        File.Copy(Path.Combine(root, "a.png"), Path.Combine(root, "nested", "ignored.png"));
        var hashes = Directory.GetFiles(root).ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var ordered = ImageFolderService.GetImageNames(root);
        Check(ordered.SequenceEqual(new[] { "a.png", "B.JPG", "c.jpeg", "d.gif", "e.bmp" }), "Filtering / alphabetical order");
        string work = ImageFolderService.GetWorkingDirectory(root);
        var paths = await ImageFolderService.ImportAsync(root, work, ordered);
        Check(paths.Count == 5 && Path.GetFileName(paths[0]) == "frame_001.png", "Names / count");
        foreach (string path in paths)
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(path);
            Check(image.Frames.Count == 1 && image.Width == 2, "PNG conversion / GIF first frame");
        }

        var state = new ProjectState
        {
            InputFilePath = root, IsFolderSource = true, WorkingDirectory = work,
            SourceImageNames = ordered,
            Frames = paths.Select((p, i) => new FrameInfo { Index = i, SourcePath = p, IsKept = i != 1, AnchorPoint = new System.Drawing.PointF(i + 1, 2), OffsetX = i }).ToList()
        };
        var persistence = new ProjectPersistenceService();
        string projectPath = Path.Combine(root, "project.json");
        await persistence.SaveAsync(state, projectPath);
        var loaded = (await persistence.LoadAsync(projectPath))!;
        Check(loaded.IsFolderSource && loaded.SourceImageNames.SequenceEqual(ordered), "Folder project round trip");
        // Delete only generated cache files, then exercise the application's recovery path.
        foreach (string path in paths) File.Delete(path);
        File.Copy(Path.Combine(root, "a.png"), Path.Combine(root, "0-new.png"));
        using var form = new MainForm();
        SynchronizationContext.SetSynchronizationContext(null);
        var rebuild = typeof(MainForm).GetMethod("EnsureProjectWorkingFilesAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)rebuild.Invoke(form, new object[] { loaded })!;
        Check(loaded.Frames.Count == 5 && loaded.Frames.All(f => File.Exists(f.SourcePath)), "Cache recovery uses original manifest");
        Check(!loaded.Frames[1].IsKept && loaded.Frames[1].AnchorPoint?.X == 2 && loaded.Frames[1].OffsetX == 1, "Recovery preserves frame settings");
        string legacyPath = Path.Combine(root, "legacy.json");
        await File.WriteAllTextAsync(legacyPath, "{\"InputFilePath\":\"movie.mp4\",\"WorkingDirectory\":\"movie_work\",\"Frames\":[]}");
        var legacy = (await persistence.LoadAsync(legacyPath))!;
        Check(!legacy.IsFolderSource && legacy.SourceImageNames.Count == 0 && legacy.InputFilePath == "movie.mp4", "Legacy project defaults");
        await persistence.SaveAsync(legacy, legacyPath);
        Check(!(await persistence.LoadAsync(legacyPath))!.IsFolderSource, "Video project round trip");
        var many = await ImageFolderService.ImportAsync(root, work, Enumerable.Repeat("a.png", 1001).ToList());
        Check(Path.GetFileName(many[999]) == "frame_1000.png" && Path.GetFileName(many[1000]) == "frame_1001.png", "Numeric order beyond 999");
        await ImageFolderService.ImportAsync(root, work, ordered);
        Check(Directory.GetFiles(Path.Combine(work, "frames"), "frame_*.png").Length == 5, "Reimport removes stale frames");
        foreach (var entry in hashes)
            Check(entry.Value == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(entry.Key))), "Source modified: " + entry.Key);
        try
        {
            await ImageFolderService.ImportAsync(root, root, ordered);
            throw new Exception("Unsafe destination accepted");
        }
        catch (InvalidOperationException) { }
        Console.WriteLine("PASS: conversion, ordering, source integrity, project persistence, legacy defaults, cache recovery, and destination safety.");
        Console.WriteLine("Test artifacts: " + root);
    }
}
