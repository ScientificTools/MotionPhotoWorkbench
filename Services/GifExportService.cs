using System;
using System.Collections.Generic;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

namespace MotionPhotoWorkbench.Services;

public sealed class GifExportService
{
    public void ExportGif(IReadOnlyList<string> framePaths, string outputGifPath, int delayCs)
    {
        if (framePaths.Count == 0)
            throw new InvalidOperationException("No images to export.");

        using SixLabors.ImageSharp.Image<Rgba32> first = SixLabors.ImageSharp.Image.Load<Rgba32>(framePaths[0]);

        first.Metadata.GetGifMetadata().RepeatCount = 0;
        first.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = delayCs;

        for (int i = 1; i < framePaths.Count; i++)
        {
            using SixLabors.ImageSharp.Image<Rgba32> img = SixLabors.ImageSharp.Image.Load<Rgba32>(framePaths[i]);
            img.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = delayCs;
            first.Frames.AddFrame(img.Frames.RootFrame);
        }

        first.Save(outputGifPath, new GifEncoder());
    }
}
