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

        WindowState = FormWindowState.Maximized;
        pictureBoxFrame.SizeMode = PictureBoxSizeMode.Zoom;
        listBoxFrames.DrawMode = DrawMode.OwnerDrawFixed;
        listBoxFrames.ItemHeight = Math.Max(listBoxFrames.Font.Height + 10, 28);
        listBoxFrames.DrawItem += listBoxFrames_DrawItem;
        btnPrev.MinimumSize = new Size(0, 40);
        btnNext.MinimumSize = new Size(0, 40);
        btnToggleKeep.MinimumSize = new Size(0, 40);
        btnPrev.Height = 40;
        btnNext.Height = 40;
        btnToggleKeep.Height = 40;
        btnToggleKeep.MinimumSize = new Size(0, 44);
        btnRenderAndExportGif.AutoSize = false;
        btnRenderAndExportGif.MinimumSize = new Size(0, 48);
        btnRenderAndExportGif.Height = 48;
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

    private void listBoxFrames_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
            return;

        int index = listBoxFrames.IndexFromPoint(e.Location);
        if (index < 0 || index >= _project.Frames.Count)
            return;

        listBoxFrames.SelectedIndex = index;
        menuToggleKeep.Text = _project.Frames[index].IsKept ? "Ecarter la frame" : "Reintegrer la frame";
    }

    private void listBoxFrames_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is not (Keys.Delete or Keys.Enter))
            return;

        ToggleSelectedFrameKeepState();
        e.Handled = true;
    }

    private void menuToggleKeep_Click(object? sender, EventArgs e)
    {
        ToggleSelectedFrameKeepState();
    }

    private void ToggleSelectedFrameKeepState()
    {
        if (listBoxFrames.SelectedIndex < 0 || listBoxFrames.SelectedIndex >= _project.Frames.Count)
            return;

        int index = listBoxFrames.SelectedIndex;
        _project.Frames[index].IsKept = !_project.Frames[index].IsKept;

        RefreshFrameList();
        listBoxFrames.SelectedIndex = index;

        if (_currentIndex == index)
            UpdateFrameInfo();
    }

    private void listBoxFrames_DrawItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index < 0 || e.Index >= _project.Frames.Count)
            return;

        FrameInfo frame = _project.Frames[e.Index];
        Color textColor = GetFrameListColor(frame);
        Color badgeColor = textColor;

        if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
        {
            using var selectionBrush = new SolidBrush(SystemColors.Highlight);
            e.Graphics.FillRectangle(selectionBrush, e.Bounds);
            textColor = Color.White;
            badgeColor = Color.White;
        }

        string fileName = Path.GetFileName(frame.SourcePath);
        string status = frame.IsKept
            ? frame.AnchorPoint.HasValue ? "POINT" : "A PLACER"
            : "ECARTEE";

        Rectangle textBounds = new(e.Bounds.X + 6, e.Bounds.Y + 1, Math.Max(0, e.Bounds.Width - 118), e.Bounds.Height - 2);
        Rectangle badgeBounds = new(e.Bounds.Right - 106, e.Bounds.Y + 1, 100, e.Bounds.Height - 2);

        TextRenderer.DrawText(
            e.Graphics,
            fileName,
            e.Font,
            textBounds,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(
            e.Graphics,
            status,
            e.Font,
            badgeBounds,
            badgeColor,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        e.DrawFocusRectangle();
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

            var renderResult = await _alignmentService.RenderAlignedFramesAsync(_project, alignedDir);
            var rendered = renderResult.FramePaths.ToList();
            _project.OutputCrop = renderResult.IntersectionCrop;
            SyncCropControls(renderResult.IntersectionCrop);

            if (rendered.Count == 0)
            {
                MessageBox.Show(this, "Aucune image exportable. Vérifie les points fixes.");
                return;
            }

            List<string> alignedBase = renderResult.FramePaths.ToList();
            Rectangle additionalCrop = new(0, 0, renderResult.IntersectionCrop.Width, renderResult.IntersectionCrop.Height);

            while (true)
            {
                PreviewForm.PreviewExportChoice exportChoice;

                lblStatus.Text = "Aperçu du crop automatique...";
                using (var previewImage = LoadPreviewImage(renderResult.PreviewPath))
                using (var previewForm = new PreviewForm(previewImage, renderResult.IntersectionCrop, additionalCrop))
                {
                    if (previewForm.ShowDialog(this) != DialogResult.OK)
                    {
                        lblStatus.Text = "Prévisualisation fermée.";
                        return;
                    }

                    exportChoice = previewForm.ExportChoice;
                    additionalCrop = previewForm.SelectedCrop;
                }

                if (additionalCrop.Width <= 0 || additionalCrop.Height <= 0)
                {
                    MessageBox.Show(this, "Le rectangle de crop additionnel est vide.");
                    continue;
                }

                List<string> exportFrames = alignedBase;
                if (additionalCrop.X != 0 || additionalCrop.Y != 0 ||
                    additionalCrop.Width != renderResult.IntersectionCrop.Width ||
                    additionalCrop.Height != renderResult.IntersectionCrop.Height)
                {
                    lblStatus.Text = "Application du crop additionnel...";
                    string finalDir = Path.Combine(_project.WorkingDirectory, "final");
                    exportFrames = (await _alignmentService.ApplyAdditionalCropAsync(alignedBase, additionalCrop, finalDir)).ToList();
                }

                _project.OutputCrop = new Rectangle(
                    renderResult.IntersectionCrop.X + additionalCrop.X,
                    renderResult.IntersectionCrop.Y + additionalCrop.Y,
                    additionalCrop.Width,
                    additionalCrop.Height);
                SyncCropControls(_project.OutputCrop);

                if (exportChoice == PreviewForm.PreviewExportChoice.Mpeg)
                {
                    using var sfd = new SaveFileDialog
                    {
                        Filter = "Vidéo MP4 (H.264)|*.mp4",
                        FileName = "animation.mp4"
                    };

                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        continue;

                    lblStatus.Text = "Création de la vidéo MP4...";
                    await _ffmpegService.ExportMpegAsync(exportFrames, sfd.FileName, _project.GifDelayCs);

                    lblStatus.Text = "Vidéo exportée.";
                    MessageBox.Show(this, "Export vidéo terminé.");
                }
                else
                {
                    using var sfd = new SaveFileDialog
                    {
                        Filter = "GIF animé|*.gif",
                        FileName = "animation.gif"
                    };

                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        continue;

                    lblStatus.Text = "Création du GIF...";
                    _gifExportService.ExportGif(exportFrames, sfd.FileName, _project.GifDelayCs);

                    lblStatus.Text = "GIF exporté.";
                    MessageBox.Show(this, "Export GIF terminé.");
                }
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
        SyncCropControls(_project.OutputCrop);
        numTargetX.Value = ClampToRange((decimal)_project.TargetAnchor.X, numTargetX.Minimum, numTargetX.Maximum);
        numTargetY.Value = ClampToRange((decimal)_project.TargetAnchor.Y, numTargetY.Minimum, numTargetY.Maximum);

        lblStatus.Text = "Projet chargé.";
    }

    private void SyncCropControls(Rectangle crop)
    {
        numCropX.Value = ClampToRange(crop.X, numCropX.Minimum, numCropX.Maximum);
        numCropY.Value = ClampToRange(crop.Y, numCropY.Minimum, numCropY.Maximum);
        numCropW.Value = ClampToRange(crop.Width, numCropW.Minimum, numCropW.Maximum);
        numCropH.Value = ClampToRange(crop.Height, numCropH.Minimum, numCropH.Maximum);
    }

    private static Bitmap LoadPreviewImage(string previewPath)
    {
        using var fs = File.OpenRead(previewPath);
        using var image = new Bitmap(fs);
        return new Bitmap(image);
    }

    private static decimal ClampToRange(decimal value, decimal minimum, decimal maximum)
    {
        if (value < minimum) return minimum;
        if (value > maximum) return maximum;
        return value;
    }

    private static Color GetFrameListColor(FrameInfo frame)
    {
        if (!frame.IsKept)
            return Color.Firebrick;

        if (frame.AnchorPoint.HasValue)
            return Color.ForestGreen;

        return Color.Black;
    }
}
