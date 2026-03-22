using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using MotionPhotoWorkbench.Models;
using MotionPhotoWorkbench.Services;
using MotionPhotoWorkbench.Utils;
using SDImage = System.Drawing.Image;
using SDPointF = System.Drawing.PointF;
using SDSize = System.Drawing.Size;

namespace MotionPhotoWorkbench;

public partial class MainForm : Form
{
    private readonly FfmpegService _ffmpegService;
    private readonly MotionPhotoService _motionPhotoService;
    private readonly ImageAlignmentService _alignmentService;
    private readonly GifExportService _gifExportService;
    private readonly ProjectPersistenceService _persistenceService;

    private ProjectState _project = new();
    private int _currentIndex = -1;
    private SDImage? _currentBitmap;

    public MainForm()
    {
        InitializeComponent();

        string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        _ffmpegService = new FfmpegService(ffmpegPath);
        _motionPhotoService = new MotionPhotoService();
        _alignmentService = new ImageAlignmentService();
        _gifExportService = new GifExportService();
        _persistenceService = new ProjectPersistenceService();

        pictureBoxFrame.SizeMode = PictureBoxSizeMode.Zoom;
        RefreshFrameList();
        UpdateFrameInfo();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        ApplyResponsiveLayout();
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        BeginInvoke(new Action(ApplyResponsiveLayout));
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Minimized)
        {
            BeginInvoke(new Action(ApplyResponsiveLayout));
        }
    }

    private void ApplyResponsiveLayout()
    {
        SetSafeSplitterDistance(splitMain, 260);

        int desiredRightToolsWidth = 360;
        int desiredDistance = splitRight.Width - desiredRightToolsWidth;
        SetSafeSplitterDistance(splitRight, desiredDistance);
    }

    private static void SetSafeSplitterDistance(SplitContainer splitContainer, int desired)
    {
        if (splitContainer.Width <= 0 || splitContainer.Height <= 0)
            return;

        int total = splitContainer.Orientation == Orientation.Vertical
            ? splitContainer.ClientSize.Width
            : splitContainer.ClientSize.Height;

        int min = splitContainer.Panel1MinSize;
        int max = total - splitContainer.Panel2MinSize - splitContainer.SplitterWidth;

        if (max < min)
            return;

        int safe = Math.Max(min, Math.Min(desired, max));

        if (splitContainer.SplitterDistance != safe)
            splitContainer.SplitterDistance = safe;
    }

    private async void btnOpenInput_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Images/Vidéos|*.jpg;*.jpeg;*.png;*.heic;*.mp4;*.mov|Tous les fichiers|*.*"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        string inputFile = ofd.FileName;
        string workDir = Path.Combine(
            Path.GetDirectoryName(inputFile)!,
            Path.GetFileNameWithoutExtension(inputFile) + "_work");

        _project = new ProjectState
        {
            InputFilePath = inputFile,
            WorkingDirectory = workDir,
            TargetAnchor = new SDPointF(150, 150),
            OutputCrop = new Rectangle(0, 0, 300, 300),
            GifDelayCs = (int)numGifDelay.Value
        };

        lblStatus.Text = "Préparation de l'extraction...";
        UseWaitCursor = true;

        try
        {
            string framesDir = Path.Combine(workDir, "frames");
            string sourceForExtraction = ResolveSourceForExtraction(inputFile, workDir, out string sourceMessage);
            lblStatus.Text = sourceMessage;
            Application.DoEvents();

            await _ffmpegService.ExtractFramesAsync(sourceForExtraction, framesDir);

            var files = Directory.GetFiles(framesDir, "*.png")
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _project.Frames = files
                .Select((path, i) => new FrameInfo
                {
                    Index = i,
                    SourcePath = path,
                    IsKept = true
                })
                .ToList();

            RefreshFrameList();

            if (_project.Frames.Count > 0)
            {
                LoadFrame(0);
                lblStatus.Text = $"Frames extraites : {_project.Frames.Count}";
            }
            else
            {
                ClearCurrentImage();
                lblStatus.Text = "Aucune frame extraite.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Erreur";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private string ResolveSourceForExtraction(string inputFile, string workDir, out string message)
    {
        string extension = Path.GetExtension(inputFile).ToLowerInvariant();
        string embeddedVideoPath = Path.Combine(workDir, "embedded_video.mp4");

        if (extension is ".jpg" or ".jpeg")
        {
            if (_motionPhotoService.TryExtractEmbeddedVideo(inputFile, embeddedVideoPath, out string motionMessage))
            {
                message = motionMessage;
                return embeddedVideoPath;
            }

            message = $"{motionMessage} Extraction directe via FFmpeg sur le fichier source.";
            return inputFile;
        }

        message = "Extraction directe via FFmpeg.";
        return inputFile;
    }

    private void RefreshFrameList()
    {
        listBoxFrames.DataSource = null;
        listBoxFrames.DataSource = _project.Frames;
    }

    private void LoadFrame(int index)
    {
        if (index < 0 || index >= _project.Frames.Count)
            return;

        _currentIndex = index;

        ClearCurrentImage();

        using var fs = File.OpenRead(_project.Frames[index].SourcePath);
        using var loaded = SDImage.FromStream(fs);
        _currentBitmap = (SDImage)loaded.Clone();
        pictureBoxFrame.Image = (SDImage)_currentBitmap.Clone();

        listBoxFrames.SelectedIndex = index;
        UpdateFrameInfo();
        pictureBoxFrame.Invalidate();
    }

    private void ClearCurrentImage()
    {
        pictureBoxFrame.Image?.Dispose();
        pictureBoxFrame.Image = null;
        _currentBitmap?.Dispose();
        _currentBitmap = null;
        _currentIndex = _project.Frames.Count == 0 ? -1 : _currentIndex;
    }

    private void UpdateFrameInfo()
    {
        if (_currentIndex < 0 || _currentIndex >= _project.Frames.Count)
        {
            lblFrameInfo.Text = "Aucune frame sélectionnée";
            return;
        }

        var frame = _project.Frames[_currentIndex];
        lblFrameInfo.Text =
            $"Frame {_currentIndex + 1}/{_project.Frames.Count} | " +
            $"Keep={frame.IsKept} | " +
            $"Anchor={(frame.AnchorPoint.HasValue ? $"{frame.AnchorPoint.Value.X:0.0},{frame.AnchorPoint.Value.Y:0.0}" : "non défini")}";
    }

    private void listBoxFrames_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (listBoxFrames.SelectedIndex >= 0 && listBoxFrames.SelectedIndex != _currentIndex)
            LoadFrame(listBoxFrames.SelectedIndex);
    }

    private void pictureBoxFrame_MouseClick(object sender, MouseEventArgs e)
    {
        if (_currentIndex < 0 || pictureBoxFrame.Image is null)
            return;

        var imgPoint = ImageViewerMath.ClientToImage(
            e.Location,
            pictureBoxFrame.ClientSize,
            pictureBoxFrame.Image.Size);

        if (imgPoint is null)
            return;

        _project.Frames[_currentIndex].AnchorPoint = imgPoint.Value;
        UpdateFrameInfo();
        pictureBoxFrame.Invalidate();
        RefreshFrameList();
        listBoxFrames.SelectedIndex = _currentIndex;
    }

    private void pictureBoxFrame_Paint(object sender, PaintEventArgs e)
    {
        if (_currentIndex < 0 || pictureBoxFrame.Image is null)
            return;

        var frame = _project.Frames[_currentIndex];
        if (!frame.AnchorPoint.HasValue)
            return;

        var clientPoint = ImageToClient(pictureBoxFrame.ClientSize, pictureBoxFrame.Image.Size, frame.AnchorPoint.Value);
        if (clientPoint is null)
            return;

        using var pen = new Pen(Color.Red, 2);
        e.Graphics.DrawLine(pen, clientPoint.Value.X - 8, clientPoint.Value.Y, clientPoint.Value.X + 8, clientPoint.Value.Y);
        e.Graphics.DrawLine(pen, clientPoint.Value.X, clientPoint.Value.Y - 8, clientPoint.Value.X, clientPoint.Value.Y + 8);
    }

    private static SDPointF? ImageToClient(SDSize clientSize, SDSize imageSize, SDPointF imagePoint)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
            return null;

        float ratioX = (float)clientSize.Width / imageSize.Width;
        float ratioY = (float)clientSize.Height / imageSize.Height;
        float scale = Math.Min(ratioX, ratioY);

        float displayedWidth = imageSize.Width * scale;
        float displayedHeight = imageSize.Height * scale;

        float offsetX = (clientSize.Width - displayedWidth) / 2f;
        float offsetY = (clientSize.Height - displayedHeight) / 2f;

        return new SDPointF(
            offsetX + imagePoint.X * scale,
            offsetY + imagePoint.Y * scale);
    }

    private void btnPrev_Click(object sender, EventArgs e)
    {
        if (_currentIndex > 0)
            LoadFrame(_currentIndex - 1);
    }

    private void btnNext_Click(object sender, EventArgs e)
    {
        if (_currentIndex < _project.Frames.Count - 1)
            LoadFrame(_currentIndex + 1);
    }

    private void btnToggleKeep_Click(object sender, EventArgs e)
    {
        if (_currentIndex < 0)
            return;

        _project.Frames[_currentIndex].IsKept = !_project.Frames[_currentIndex].IsKept;
        UpdateFrameInfo();
        RefreshFrameList();
        listBoxFrames.SelectedIndex = _currentIndex;
    }

    private void btnCopyAnchorToAll_Click(object sender, EventArgs e)
    {
        if (_currentIndex < 0)
            return;

        var anchor = _project.Frames[_currentIndex].AnchorPoint;
        if (!anchor.HasValue)
        {
            MessageBox.Show(this, "Aucun point fixe défini sur la frame courante.");
            return;
        }

        foreach (var frame in _project.Frames)
            frame.AnchorPoint = anchor;

        UpdateFrameInfo();
        RefreshFrameList();
        listBoxFrames.SelectedIndex = _currentIndex;
    }

    private async void btnRenderAndExportGif_Click(object sender, EventArgs e)
    {
        try
        {
            if (_project.Frames.Count == 0)
            {
                MessageBox.Show(this, "Aucune frame chargée.");
                return;
            }

            _project.GifDelayCs = (int)numGifDelay.Value;
            _project.OutputCrop = new Rectangle(
                (int)numCropX.Value,
                (int)numCropY.Value,
                (int)numCropW.Value,
                (int)numCropH.Value);

            _project.TargetAnchor = new SDPointF(
                (float)numTargetX.Value,
                (float)numTargetY.Value);

            _alignmentService.ComputeOffsets(_project);

            string alignedDir = Path.Combine(_project.WorkingDirectory, "aligned");
            lblStatus.Text = "Rendu des images alignées...";
            UseWaitCursor = true;

            var rendered = await _alignmentService.RenderAlignedFramesAsync(_project, alignedDir);

            if (rendered.Count == 0)
            {
                MessageBox.Show(this, "Aucune image exportable. Vérifie les points fixes.");
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "GIF animé|*.gif",
                FileName = "animation.gif"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            lblStatus.Text = "Création du GIF...";
            _gifExportService.ExportGif(rendered, sfd.FileName, _project.GifDelayCs);

            lblStatus.Text = "GIF exporté.";
            MessageBox.Show(this, "Export terminé.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Erreur";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async void btnSaveProject_Click(object sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Projet JSON|*.json",
            FileName = "project.json"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        await _persistenceService.SaveAsync(_project, sfd.FileName);
        lblStatus.Text = "Projet sauvegardé.";
    }

    private async void btnLoadProject_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Projet JSON|*.json"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        var loaded = await _persistenceService.LoadAsync(ofd.FileName);
        if (loaded is null)
        {
            MessageBox.Show(this, "Impossible de charger le projet.");
            return;
        }

        _project = loaded;
        RefreshFrameList();

        if (_project.Frames.Count > 0)
            LoadFrame(0);
        else
            ClearCurrentImage();

        numGifDelay.Value = Math.Clamp(_project.GifDelayCs, numGifDelay.Minimum, numGifDelay.Maximum);
        numCropX.Value = ClampToRange(_project.OutputCrop.X, numCropX.Minimum, numCropX.Maximum);
        numCropY.Value = ClampToRange(_project.OutputCrop.Y, numCropY.Minimum, numCropY.Maximum);
        numCropW.Value = ClampToRange(_project.OutputCrop.Width, numCropW.Minimum, numCropW.Maximum);
        numCropH.Value = ClampToRange(_project.OutputCrop.Height, numCropH.Minimum, numCropH.Maximum);
        numTargetX.Value = ClampToRange((decimal)_project.TargetAnchor.X, numTargetX.Minimum, numTargetX.Maximum);
        numTargetY.Value = ClampToRange((decimal)_project.TargetAnchor.Y, numTargetY.Minimum, numTargetY.Maximum);

        lblStatus.Text = "Projet chargé.";
    }

    private static decimal ClampToRange(decimal value, decimal minimum, decimal maximum)
    {
        if (value < minimum) return minimum;
        if (value > maximum) return maximum;
        return value;
    }
}
