using System.Collections.Generic;
using System.Drawing;

namespace MotionPhotoWorkbench.Models;

public sealed class ProjectState
{
    public string InputFilePath { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public List<FrameInfo> Frames { get; set; } = new();

    // Point final commun dans l'image recadrée
    public PointF TargetAnchor { get; set; } = new(150, 150);

    // Rectangle de sortie final
    public Rectangle OutputCrop { get; set; } = new(0, 0, 300, 300);

    // Cadence d'export video/GIF
    public int VideoFps { get; set; } = 20;
}


