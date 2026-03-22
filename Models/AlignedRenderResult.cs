using System.Collections.Generic;
using System.Drawing;

namespace MotionPhotoWorkbench.Models;

public sealed class AlignedRenderResult
{
    public required IReadOnlyList<string> FramePaths { get; init; }
    public required Rectangle IntersectionCrop { get; init; }
    public required string PreviewPath { get; init; }
}
