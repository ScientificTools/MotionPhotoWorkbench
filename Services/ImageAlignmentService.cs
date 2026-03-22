using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MotionPhotoWorkbench.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SDRectangle = System.Drawing.Rectangle;

namespace MotionPhotoWorkbench.Services;

public sealed class ImageAlignmentService
{
    public void ComputeOffsets(ProjectState project)
    {
        foreach (var frame in project.Frames.Where(f => f.IsKept && f.AnchorPoint.HasValue))
        {
            frame.OffsetX = project.TargetAnchor.X - frame.AnchorPoint!.Value.X;
            frame.OffsetY = project.TargetAnchor.Y - frame.AnchorPoint!.Value.Y;
        }
    }

    public async Task<List<string>> RenderAlignedFramesAsync(
        ProjectState project,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var outputs = new List<string>();

        foreach (var frame in project.Frames.Where(f => f.IsKept))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!frame.AnchorPoint.HasValue)
                continue;

            string outputPath = Path.Combine(outputDirectory, $"aligned_{frame.Index:0000}.png");
            await Task.Run(() => RenderOne(frame, project, outputPath), cancellationToken);
            outputs.Add(outputPath);
        }

        return outputs;
    }

    private static void RenderOne(FrameInfo frame, ProjectState project, string outputPath)
    {
        using SixLabors.ImageSharp.Image<Rgba32> source = SixLabors.ImageSharp.Image.Load<Rgba32>(frame.SourcePath);

        SDRectangle crop = project.OutputCrop;

        using SixLabors.ImageSharp.Image<Rgba32> canvas = new(crop.Width, crop.Height);

        int drawX = (int)MathF.Round(frame.OffsetX - crop.X);
        int drawY = (int)MathF.Round(frame.OffsetY - crop.Y);

        canvas.Mutate(ctx =>
        {
            ctx.DrawImage(source, new SixLabors.ImageSharp.Point(drawX, drawY), 1f);
        });

        canvas.Save(outputPath);
    }
}
