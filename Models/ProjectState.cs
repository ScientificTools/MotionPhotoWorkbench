using System.Collections.Generic;
using System.Drawing;

namespace MotionPhotoWorkbench.Models;

public sealed class ProjectState
{
    public string InputFilePath { get; set; } = string.Empty;
    // Missing in older projects: false preserves the video/motion-photo workflow.
    public bool IsFolderSource { get; set; }
    public List<string> SourceImageNames { get; set; } = new();
    public string WorkingDirectory { get; set; } = string.Empty;
    public List<FrameInfo> Frames { get; set; } = new();
    public ImageAdjustmentSettings Adjustments { get; set; } = ImageAdjustmentSettings.Default;

    // Point final commun dans l'image recadrée
    public PointF TargetAnchor { get; set; } = new(150, 150);

    // Rectangle de sortie final
    public Rectangle OutputCrop { get; set; } = new(0, 0, 300, 300);

    // Cadence d'export video/GIF
    public int VideoFps { get; set; } = 20;

    // Score minimal (0-1) sous lequel l'auto-anchor abandonne la frame candidate
    public float AutoAnchorStopConfidence { get; set; } = 0.82f;

    // Score minimal (0-1) sous lequel une frame acceptée par l'auto-anchor est marquée incertaine (orange)
    public float AutoAnchorDoubtConfidence { get; set; } = 0.95f;

    // Rejoue la séquence en aller-retour pour éviter le saut d'image en boucle
    public bool PingPongPlayback { get; set; }

    // N'utilise qu'une image sur deux pour accélérer le défilement
    public bool HalfFrameRate { get; set; }
}


