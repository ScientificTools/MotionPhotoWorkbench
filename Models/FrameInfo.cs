using System;
using System.Drawing;

namespace MotionPhotoWorkbench.Models;

public sealed class FrameInfo
{
    public int Index { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public bool IsKept { get; set; } = true;

    // Point choisi par l'utilisateur dans l'image source
    public PointF? AnchorPoint { get; set; }

    // Vrai si l'ancrage a été accepté par l'auto-anchor avec un score sous le seuil de confiance "sûr"
    public bool IsAnchorUncertain { get; set; }

    // Décalage calculé pour alignement
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }

    public override string ToString()
    {
        return $"#{Index:000} {(IsKept ? "[X]" : "[ ]")} {Path.GetFileName(SourcePath)}";
    }
}
