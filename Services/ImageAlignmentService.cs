using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MotionPhotoWorkbench.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ISRectangle = SixLabors.ImageSharp.Rectangle;
using SDRectangle = System.Drawing.Rectangle;

namespace MotionPhotoWorkbench.Services;

public sealed class ImageAlignmentService
{
    private readonly ImageAdjustmentService _imageAdjustmentService = new();

    public SDRectangle ComputeIntersectionCrop(ProjectState project)
    {
        var alignedFrames = GetRenderableFrames(project).ToList();
        if (alignedFrames.Count == 0)
            throw new InvalidOperationException("No usable frame: at least one kept image with an anchor point is required.");

        SDRectangle intersection = alignedFrames[0].VisibleArea;

        foreach (var frame in alignedFrames.Skip(1))
        {
            intersection = SDRectangle.Intersect(intersection, frame.VisibleArea);
            if (intersection.Width <= 0 || intersection.Height <= 0)
                throw new InvalidOperationException("The visible areas do not share any common intersection with the current anchor points.");
        }

        return intersection;
    }

    public void ComputeOffsets(ProjectState project)
    {
        foreach (var frame in project.Frames.Where(f => f.IsKept && f.AnchorPoint.HasValue))
        {
            frame.OffsetX = project.TargetAnchor.X - frame.AnchorPoint!.Value.X;
            frame.OffsetY = project.TargetAnchor.Y - frame.AnchorPoint!.Value.Y;
        }
    }

    public async Task<AlignedRenderResult> RenderAlignedFramesAsync(
        ProjectState project,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var outputs = new List<string>();
        SDRectangle intersectionCrop = ComputeIntersectionCrop(project);

        foreach (var frame in project.Frames.Where(f => f.IsKept))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!frame.AnchorPoint.HasValue)
                continue;

            string outputPath = Path.Combine(outputDirectory, $"aligned_{frame.Index:0000}.png");
            if (project.Adjustments is null)
                project.Adjustments = ImageAdjustmentSettings.Default;

            ImageAdjustmentSettings adjustments = project.Adjustments.Clone();
            await Task.Run(() => RenderOne(frame, intersectionCrop, outputPath, adjustments), cancellationToken);
            outputs.Add(outputPath);
        }

        if (outputs.Count == 0)
            throw new InvalidOperationException("No exportable images. Check the anchor points.");

        string previewPath = Path.Combine(outputDirectory, "preview_merged.png");
        await Task.Run(() => BuildMergedPreview(outputs, previewPath), cancellationToken);

        return new AlignedRenderResult
        {
            FramePaths = outputs,
            IntersectionCrop = intersectionCrop,
            PreviewPath = previewPath
        };
    }

    private static IEnumerable<(FrameInfo Frame, SDRectangle VisibleArea)> GetRenderableFrames(ProjectState project)
    {
        foreach (var frame in project.Frames.Where(f => f.IsKept && f.AnchorPoint.HasValue))
        {
            var info = SixLabors.ImageSharp.Image.Identify(frame.SourcePath)
                ?? throw new InvalidOperationException($"Unable to identify source image '{frame.SourcePath}'.");

            int drawX = (int)MathF.Round(frame.OffsetX);
            int drawY = (int)MathF.Round(frame.OffsetY);

            yield return (frame, new SDRectangle(drawX, drawY, info.Width, info.Height));
        }
    }

    private void RenderOne(FrameInfo frame, SDRectangle crop, string outputPath, ImageAdjustmentSettings adjustments)
    {
        using SixLabors.ImageSharp.Image<Rgba32> source = SixLabors.ImageSharp.Image.Load<Rgba32>(frame.SourcePath);
        _imageAdjustmentService.ApplyAdjustments(source, adjustments);

        using SixLabors.ImageSharp.Image<Rgba32> canvas = new(crop.Width, crop.Height);

        int drawX = (int)MathF.Round(frame.OffsetX) - crop.X;
        int drawY = (int)MathF.Round(frame.OffsetY) - crop.Y;

        canvas.Mutate(ctx =>
        {
            ctx.DrawImage(source, new SixLabors.ImageSharp.Point(drawX, drawY), 1f);
        });

        canvas.Save(outputPath);
    }

    private static void BuildMergedPreview(IReadOnlyList<string> framePaths, string outputPath)
    {
        using SixLabors.ImageSharp.Image<Rgba32> first = SixLabors.ImageSharp.Image.Load<Rgba32>(framePaths[0]);
        using SixLabors.ImageSharp.Image<Rgba32> preview = new(first.Width, first.Height);

        long[] sumR = new long[first.Width * first.Height];
        long[] sumG = new long[first.Width * first.Height];
        long[] sumB = new long[first.Width * first.Height];
        long[] sumA = new long[first.Width * first.Height];

        foreach (string framePath in framePaths)
        {
            using SixLabors.ImageSharp.Image<Rgba32> frame = SixLabors.ImageSharp.Image.Load<Rgba32>(framePath);

            if (frame.Width != preview.Width || frame.Height != preview.Height)
                throw new InvalidOperationException("Aligned images do not all have the same size.");

            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    int index = (y * frame.Width) + x;
                    Rgba32 pixel = frame[x, y];
                    sumR[index] += pixel.R;
                    sumG[index] += pixel.G;
                    sumB[index] += pixel.B;
                    sumA[index] += pixel.A;
                }
            }
        }

        int divisor = framePaths.Count;
        for (int y = 0; y < preview.Height; y++)
        {
            for (int x = 0; x < preview.Width; x++)
            {
                int index = (y * preview.Width) + x;
                preview[x, y] = new Rgba32(
                    (byte)(sumR[index] / divisor),
                    (byte)(sumG[index] / divisor),
                    (byte)(sumB[index] / divisor),
                    (byte)(sumA[index] / divisor));
            }
        }

        preview.Save(outputPath);
    }

    public async Task<IReadOnlyList<string>> ApplyAdditionalCropAsync(
        IReadOnlyList<string> framePaths,
        SDRectangle crop,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (framePaths.Count == 0)
            throw new InvalidOperationException("No images to crop.");

        if (crop.Width <= 0 || crop.Height <= 0)
            throw new InvalidOperationException("The additional crop must have a positive size.");

        Directory.CreateDirectory(outputDirectory);

        var outputs = new List<string>(framePaths.Count);
        for (int i = 0; i < framePaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string outputPath = Path.Combine(outputDirectory, $"final_{i:0000}.png");
            string sourcePath = framePaths[i];
            await Task.Run(() => CropOne(sourcePath, crop, outputPath), cancellationToken);
            outputs.Add(outputPath);
        }

        return outputs;
    }

    private static void CropOne(string sourcePath, SDRectangle crop, string outputPath)
    {
        using SixLabors.ImageSharp.Image<Rgba32> source = SixLabors.ImageSharp.Image.Load<Rgba32>(sourcePath);
        var sourceBounds = new SDRectangle(0, 0, source.Width, source.Height);
        var effectiveCrop = SDRectangle.Intersect(sourceBounds, crop);

        if (effectiveCrop.Width <= 0 || effectiveCrop.Height <= 0)
            throw new InvalidOperationException("The additional crop falls completely outside the image.");

        source.Mutate(ctx => ctx.Crop(new ISRectangle(effectiveCrop.X, effectiveCrop.Y, effectiveCrop.Width, effectiveCrop.Height)));
        source.Save(outputPath, new PngEncoder());
    }
}
