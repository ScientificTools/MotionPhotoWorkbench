using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using MotionPhotoWorkbench.Models;
using MotionPhotoWorkbench.Services;
using MotionPhotoWorkbench.Utils;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ISImage = SixLabors.ImageSharp.Image;
using ISImageRgba32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using SDImage = System.Drawing.Image;
using SDPointF = System.Drawing.PointF;

namespace MotionPhotoWorkbench;

public partial class MainForm : Form
{
    private readonly FfmpegService _ffmpegService;
    private readonly MotionPhotoService _motionPhotoService;
    private readonly ImageAlignmentService _alignmentService;
    private readonly ImageAdjustmentService _imageAdjustmentService;
    private readonly GifExportService _gifExportService;
    private readonly ProjectPersistenceService _persistenceService;
    private readonly System.Windows.Forms.Timer _adjustmentPreviewTimer;

    private ProjectState _project = new();
    private int _currentIndex = -1;
    private SDImage? _currentBitmap;
    private bool _isRefreshingFrameList;
    private float _imageZoom = 1f;
    private SDPointF _imagePanOffset = SDPointF.Empty;
    private bool _isImagePointerDown;
    private bool _isImagePanning;
    private Point _imagePointerDownLocation;
    private SDPointF _imagePanOffsetAtPointerDown;
    private bool _isSyncingAdjustmentControls;
    private bool _isAdjustingTrackBar;
    private int _framePreviewRequestId;
    private CancellationTokenSource? _framePreviewCts;

    private const float MinImageZoom = 1f;
    private const float MaxImageZoom = 16f;
    private const float ImageZoomFactor = 1.25f;
    private const int ImagePanThreshold = 4;
    private const int AdjustmentDebounceMs = 120;

    public MainForm()
    {
        InitializeComponent();

        string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        _ffmpegService = new FfmpegService(ffmpegPath);
        _motionPhotoService = new MotionPhotoService();
        _alignmentService = new ImageAlignmentService();
        _imageAdjustmentService = new ImageAdjustmentService();
        _gifExportService = new GifExportService();
        _persistenceService = new ProjectPersistenceService();
        _adjustmentPreviewTimer = new System.Windows.Forms.Timer { Interval = AdjustmentDebounceMs };

        WindowState = FormWindowState.Maximized;
        pictureBoxFrame.SizeMode = PictureBoxSizeMode.Normal;
        pictureBoxFrame.MouseEnter += (_, _) => pictureBoxFrame.Focus();
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
        _adjustmentPreviewTimer.Tick += AdjustmentPreviewTimer_Tick;
        WireAdjustmentEvents();
        SyncAdjustmentControlsFromProject();
        UpdateZoomButtons();
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
            ClampImagePanOffset();
            pictureBoxFrame.Invalidate();
        }
    }

    private void ApplyResponsiveLayout()
    {
        Rectangle workingArea = Screen.FromControl(this).WorkingArea;
        int effectiveWidth = Math.Max(workingArea.Width, splitMain.ClientSize.Width);

        int desiredFramesWidth = (int)Math.Round(effectiveWidth * 0.15f);
        int desiredToolsWidth = (int)Math.Round(effectiveWidth * 0.25f);

        SetSafeSplitterDistance(splitMain, desiredFramesWidth);

        int desiredRightImageWidth = Math.Max(0, splitRight.ClientSize.Width - desiredToolsWidth);
        SetSafeSplitterDistance(splitRight, desiredRightImageWidth);
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
            VideoFps = 20
        };
        NormalizeProjectState(_project);
        SyncAdjustmentControlsFromProject();

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

            var validFiles = await Task.Run(() => FilterReadableFrames(files));

            _project.Frames = validFiles
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
                BeginInvoke(new Action(() => LoadFrame(0)));
                int skippedCount = files.Count - validFiles.Count;
                lblStatus.Text = skippedCount > 0
                    ? $"Frames extraites : {_project.Frames.Count} ({skippedCount} ignorée(s), image(s) illisible(s))"
                    : $"Frames extraites : {_project.Frames.Count}";
            }
            else
            {
                ClearCurrentImage();
                lblStatus.Text = files.Count > 0
                    ? "Aucune frame exploitable : toutes les images extraites sont illisibles."
                    : "Aucune frame extraite.";
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
        int selectedIndex = listBoxFrames.SelectedIndex;
        _isRefreshingFrameList = true;

        try
        {
            listBoxFrames.BeginUpdate();
            listBoxFrames.Items.Clear();

            foreach (var frame in _project.Frames)
                listBoxFrames.Items.Add(frame);

            if (_project.Frames.Count == 0)
            {
                listBoxFrames.ClearSelected();
            }
            else
            {
                int targetIndex = selectedIndex >= 0 && selectedIndex < _project.Frames.Count
                    ? selectedIndex
                    : Math.Clamp(_currentIndex, 0, _project.Frames.Count - 1);

                if (targetIndex >= 0)
                    listBoxFrames.SelectedIndex = targetIndex;
            }
        }
        finally
        {
            listBoxFrames.EndUpdate();
            _isRefreshingFrameList = false;
        }
    }

    private void LoadFrame(int index)
    {
        if (index < 0 || index >= _project.Frames.Count)
            return;

        _currentIndex = index;

        ClearCurrentImage();

        listBoxFrames.SelectedIndex = index;
        UpdateFrameInfo();
        QueueCurrentFrameRefresh(immediate: true);
    }

    private void ClearCurrentImage()
    {
        _adjustmentPreviewTimer.Stop();
        CancelPendingFramePreview();
        _currentBitmap?.Dispose();
        _currentBitmap = null;
        ResetImageViewport();
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
        if (_isRefreshingFrameList)
            return;

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

        Rectangle textBounds = new(e.Bounds.X + 6, e.Bounds.Y + 1, Math.Max(0, e.Bounds.Width - 142), e.Bounds.Height - 2);
        Rectangle badgeBounds = new(e.Bounds.Right - 130, e.Bounds.Y + 1, 124, e.Bounds.Height - 2);

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

    private void pictureBoxFrame_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_currentIndex < 0 || _currentBitmap is null)
            return;

        pictureBoxFrame.Focus();

        if (e.Button != MouseButtons.Left)
            return;

        _isImagePointerDown = true;
        _isImagePanning = false;
        _imagePointerDownLocation = e.Location;
        _imagePanOffsetAtPointerDown = _imagePanOffset;
        pictureBoxFrame.Capture = true;
    }

    private void pictureBoxFrame_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isImagePointerDown || _currentBitmap is null)
            return;

        if (!_isImagePanning)
        {
            int dx = e.Location.X - _imagePointerDownLocation.X;
            int dy = e.Location.Y - _imagePointerDownLocation.Y;
            if (Math.Abs(dx) < ImagePanThreshold && Math.Abs(dy) < ImagePanThreshold)
                return;

            _isImagePanning = true;
            pictureBoxFrame.Cursor = Cursors.SizeAll;
        }

        _imagePanOffset = new SDPointF(
            _imagePanOffsetAtPointerDown.X + (e.Location.X - _imagePointerDownLocation.X),
            _imagePanOffsetAtPointerDown.Y + (e.Location.Y - _imagePointerDownLocation.Y));

        ClampImagePanOffset();
        pictureBoxFrame.Invalidate();
    }

    private void pictureBoxFrame_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isImagePointerDown)
            return;

        bool wasPanning = _isImagePanning;
        _isImagePointerDown = false;
        _isImagePanning = false;
        pictureBoxFrame.Capture = false;
        pictureBoxFrame.Cursor = Cursors.Default;

        if (e.Button != MouseButtons.Left || wasPanning || _currentBitmap is null)
            return;

        var imgPoint = ImageViewerMath.ClientToImage(
            e.Location,
            pictureBoxFrame.ClientSize,
            _currentBitmap.Size,
            _imageZoom,
            _imagePanOffset);

        if (imgPoint is null)
            return;

        _project.Frames[_currentIndex].AnchorPoint = imgPoint.Value;
        UpdateFrameInfo();
        pictureBoxFrame.Invalidate();
        RefreshFrameList();
        listBoxFrames.SelectedIndex = _currentIndex;
    }

    private void pictureBoxFrame_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_currentBitmap is null)
            return;

        float factor = e.Delta > 0 ? ImageZoomFactor : 1f / ImageZoomFactor;
        SetImageZoom(_imageZoom * factor, e.Location);
    }

    private void pictureBoxFrame_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Black);

        if (_currentIndex < 0 || _currentBitmap is null)
            return;

        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        RectangleF imageBounds = ImageViewerMath.GetImageDisplayBounds(
            pictureBoxFrame.ClientSize,
            _currentBitmap.Size,
            _imageZoom,
            _imagePanOffset);

        if (imageBounds.IsEmpty || imageBounds.Width <= 0 || imageBounds.Height <= 0)
            return;

        e.Graphics.DrawImage(_currentBitmap, imageBounds);

        var frame = _project.Frames[_currentIndex];
        if (!frame.AnchorPoint.HasValue)
            return;

        var clientPoint = ImageViewerMath.ImageToClient(
            pictureBoxFrame.ClientSize,
            _currentBitmap.Size,
            frame.AnchorPoint.Value,
            _imageZoom,
            _imagePanOffset);

        if (clientPoint is null)
            return;

        float crossHalfSize = Math.Clamp(8f + ((_imageZoom - 1f) * 3f), 8f, 28f);
        float penWidth = Math.Clamp(2f + ((_imageZoom - 1f) * 0.35f), 2f, 5f);

        using var outlinePen = new Pen(Color.White, penWidth + 2f);
        using var pen = new Pen(Color.Red, penWidth);
        e.Graphics.DrawLine(outlinePen, clientPoint.Value.X - crossHalfSize, clientPoint.Value.Y, clientPoint.Value.X + crossHalfSize, clientPoint.Value.Y);
        e.Graphics.DrawLine(outlinePen, clientPoint.Value.X, clientPoint.Value.Y - crossHalfSize, clientPoint.Value.X, clientPoint.Value.Y + crossHalfSize);
        e.Graphics.DrawLine(pen, clientPoint.Value.X - crossHalfSize, clientPoint.Value.Y, clientPoint.Value.X + crossHalfSize, clientPoint.Value.Y);
        e.Graphics.DrawLine(pen, clientPoint.Value.X, clientPoint.Value.Y - crossHalfSize, clientPoint.Value.X, clientPoint.Value.Y + crossHalfSize);
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

    private void btnZoomIn_Click(object? sender, EventArgs e)
    {
        if (_currentBitmap is null)
            return;

        SetImageZoom(_imageZoom * ImageZoomFactor, GetViewportCenter());
    }

    private void btnZoomOut_Click(object? sender, EventArgs e)
    {
        if (_currentBitmap is null)
            return;

        SetImageZoom(_imageZoom / ImageZoomFactor, GetViewportCenter());
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
                using (var previewForm = new PreviewForm(previewImage, renderResult.IntersectionCrop, _project.VideoFps, additionalCrop))
                {
                    if (previewForm.ShowDialog(this) != DialogResult.OK)
                    {
                        lblStatus.Text = "Prévisualisation fermée.";
                        return;
                    }

                    exportChoice = previewForm.ExportChoice;
                    additionalCrop = previewForm.SelectedCrop;
                    _project.VideoFps = previewForm.ExportFps;
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
                    await _ffmpegService.ExportMpegAsync(exportFrames, sfd.FileName, _project.VideoFps, _project.WorkingDirectory);

                    lblStatus.Text = "Vidéo exportée.";
                    MessageBox.Show(this, "Export vidéo terminé.");
                }
                else if (exportChoice == PreviewForm.PreviewExportChoice.WebM)
                {
                    using var sfd = new SaveFileDialog
                    {
                        Filter = "VidÃ©o WebM (VP9)|*.webm",
                        FileName = "animation.webm"
                    };

                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        continue;

                    lblStatus.Text = "CrÃ©ation de la vidÃ©o WebM...";
                    await _ffmpegService.ExportWebMAsync(exportFrames, sfd.FileName, _project.VideoFps, _project.WorkingDirectory);

                    lblStatus.Text = "VidÃ©o WebM exportÃ©e.";
                    MessageBox.Show(this, "Export WebM terminÃ©.");
                }
                else if (exportChoice == PreviewForm.PreviewExportChoice.WebP)
                {
                    using var sfd = new SaveFileDialog
                    {
                        Filter = "WebP animÃ©|*.webp",
                        FileName = "animation.webp"
                    };

                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        continue;

                    lblStatus.Text = "CrÃ©ation du WebP animÃ©...";
                    await _ffmpegService.ExportAnimatedWebpAsync(exportFrames, sfd.FileName, _project.VideoFps, _project.WorkingDirectory);

                    lblStatus.Text = "WebP animÃ© exportÃ©.";
                    MessageBox.Show(this, "Export WebP terminÃ©.");
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
                    _gifExportService.ExportGif(exportFrames, sfd.FileName, ConvertFpsToGifDelayCs(_project.VideoFps));

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
        NormalizeProjectState(loaded);

        UseWaitCursor = true;
        lblStatus.Text = "Chargement du projet...";

        try
        {
            await EnsureProjectWorkingFilesAsync(loaded);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Erreur";
            return;
        }
        finally
        {
            UseWaitCursor = false;
        }

        _project = loaded;
        SyncAdjustmentControlsFromProject();
        RefreshFrameList();

        if (_project.Frames.Count > 0)
            LoadFrame(0);
        else
            ClearCurrentImage();

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

    private async Task EnsureProjectWorkingFilesAsync(ProjectState project)
    {
        if (string.IsNullOrWhiteSpace(project.InputFilePath))
            throw new InvalidOperationException("Le projet ne contient pas de chemin d'image source.");

        if (!File.Exists(project.InputFilePath))
            throw new FileNotFoundException($"Image source introuvable : {project.InputFilePath}");

        if (string.IsNullOrWhiteSpace(project.WorkingDirectory))
        {
            project.WorkingDirectory = Path.Combine(
                Path.GetDirectoryName(project.InputFilePath)!,
                Path.GetFileNameWithoutExtension(project.InputFilePath) + "_work");
        }

        if (ProjectHasUsableFrames(project))
            return;

        lblStatus.Text = "Reconstruction du repertoire de travail...";

        List<FrameInfo> savedFrames = project.Frames
            .Select(frame => new FrameInfo
            {
                Index = frame.Index,
                SourcePath = frame.SourcePath,
                IsKept = frame.IsKept,
                AnchorPoint = frame.AnchorPoint,
                OffsetX = frame.OffsetX,
                OffsetY = frame.OffsetY
            })
            .ToList();

        string framesDir = Path.Combine(project.WorkingDirectory, "frames");
        string sourceForExtraction = ResolveSourceForExtraction(project.InputFilePath, project.WorkingDirectory, out string sourceMessage);
        lblStatus.Text = $"{sourceMessage} Reconstruction des frames...";
        Application.DoEvents();

        await _ffmpegService.ExtractFramesAsync(sourceForExtraction, framesDir);

        var files = Directory.GetFiles(framesDir, "*.png")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var validFiles = await Task.Run(() => FilterReadableFrames(files));
        var savedByIndex = savedFrames.ToDictionary(frame => frame.Index);

        project.Frames = validFiles
            .Select((path, i) =>
            {
                if (savedByIndex.TryGetValue(i, out FrameInfo? savedFrame))
                {
                    return new FrameInfo
                    {
                        Index = i,
                        SourcePath = path,
                        IsKept = savedFrame.IsKept,
                        AnchorPoint = savedFrame.AnchorPoint,
                        OffsetX = savedFrame.OffsetX,
                        OffsetY = savedFrame.OffsetY
                    };
                }

                return new FrameInfo
                {
                    Index = i,
                    SourcePath = path,
                    IsKept = true
                };
            })
            .ToList();
    }

    private static bool ProjectHasUsableFrames(ProjectState project)
    {
        if (project.Frames.Count == 0)
            return false;

        return project.Frames.All(frame =>
            !string.IsNullOrWhiteSpace(frame.SourcePath) &&
            File.Exists(frame.SourcePath));
    }

    private static List<string> FilterReadableFrames(IReadOnlyList<string> files)
    {
        var readable = new List<string>(files.Count);

        foreach (string file in files)
        {
            try
            {
                var info = ISImage.Identify(file);
                if (info is not null && info.Width > 0 && info.Height > 0)
                    readable.Add(file);
            }
            catch
            {
                // Ignore unreadable frames to avoid freezing later when displaying them.
            }
        }

        return readable;
    }

    private Bitmap LoadDisplayBitmap(string sourcePath, ImageAdjustmentSettings adjustments)
    {
        using ISImageRgba32 image = ISImage.Load<Rgba32>(sourcePath);
        _imageAdjustmentService.ApplyAdjustments(image, adjustments);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        ms.Position = 0;
        using var bitmap = new Bitmap(ms);
        return new Bitmap(bitmap);
    }

    private void WireAdjustmentEvents()
    {
        trackBrightness.ValueChanged += AdjustmentTrackBar_ValueChanged;
        trackContrast.ValueChanged += AdjustmentTrackBar_ValueChanged;
        trackSaturation.ValueChanged += AdjustmentTrackBar_ValueChanged;
        trackTemperature.ValueChanged += AdjustmentTrackBar_ValueChanged;
        trackSharpness.ValueChanged += AdjustmentTrackBar_ValueChanged;
        trackHighlights.ValueChanged += AdjustmentTrackBar_ValueChanged;
        trackShadows.ValueChanged += AdjustmentTrackBar_ValueChanged;
        trackBrightness.MouseDown += AdjustmentTrackBar_MouseDown;
        trackContrast.MouseDown += AdjustmentTrackBar_MouseDown;
        trackSaturation.MouseDown += AdjustmentTrackBar_MouseDown;
        trackTemperature.MouseDown += AdjustmentTrackBar_MouseDown;
        trackSharpness.MouseDown += AdjustmentTrackBar_MouseDown;
        trackHighlights.MouseDown += AdjustmentTrackBar_MouseDown;
        trackShadows.MouseDown += AdjustmentTrackBar_MouseDown;
        trackBrightness.MouseUp += AdjustmentTrackBar_MouseUp;
        trackContrast.MouseUp += AdjustmentTrackBar_MouseUp;
        trackSaturation.MouseUp += AdjustmentTrackBar_MouseUp;
        trackTemperature.MouseUp += AdjustmentTrackBar_MouseUp;
        trackSharpness.MouseUp += AdjustmentTrackBar_MouseUp;
        trackHighlights.MouseUp += AdjustmentTrackBar_MouseUp;
        trackShadows.MouseUp += AdjustmentTrackBar_MouseUp;
        trackBrightness.KeyUp += AdjustmentTrackBar_KeyUp;
        trackContrast.KeyUp += AdjustmentTrackBar_KeyUp;
        trackSaturation.KeyUp += AdjustmentTrackBar_KeyUp;
        trackTemperature.KeyUp += AdjustmentTrackBar_KeyUp;
        trackSharpness.KeyUp += AdjustmentTrackBar_KeyUp;
        trackHighlights.KeyUp += AdjustmentTrackBar_KeyUp;
        trackShadows.KeyUp += AdjustmentTrackBar_KeyUp;
        WireAdjustmentValueTextBox(txtBrightnessValue, trackBrightness, allowNegative: true);
        WireAdjustmentValueTextBox(txtContrastValue, trackContrast, allowNegative: true);
        WireAdjustmentValueTextBox(txtSaturationValue, trackSaturation, allowNegative: true);
        WireAdjustmentValueTextBox(txtTemperatureValue, trackTemperature, allowNegative: true);
        WireAdjustmentValueTextBox(txtSharpnessValue, trackSharpness, allowNegative: false);
        WireAdjustmentValueTextBox(txtHighlightsValue, trackHighlights, allowNegative: true);
        WireAdjustmentValueTextBox(txtShadowsValue, trackShadows, allowNegative: true);
        btnResetAdjustments.Click += btnResetAdjustments_Click;
    }

    private void AdjustmentTrackBar_ValueChanged(object? sender, EventArgs e)
    {
        UpdateAdjustmentValueLabels();

        if (_isSyncingAdjustmentControls)
            return;

        _project.Adjustments = ReadAdjustmentsFromControls();

        if (!_isAdjustingTrackBar)
            QueueCurrentFrameRefresh(immediate: true);
    }

    private void btnResetAdjustments_Click(object? sender, EventArgs e)
    {
        _project.Adjustments = ImageAdjustmentSettings.Default;
        SyncAdjustmentControlsFromProject();
        QueueCurrentFrameRefresh(immediate: true);
    }

    private void AdjustmentTrackBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _isAdjustingTrackBar = true;
    }

    private void AdjustmentTrackBar_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        CommitTrackBarAdjustment();
    }

    private void AdjustmentTrackBar_KeyUp(object? sender, KeyEventArgs e)
    {
        CommitTrackBarAdjustment();
    }

    private void CommitTrackBarAdjustment()
    {
        _isAdjustingTrackBar = false;
        _project.Adjustments = ReadAdjustmentsFromControls();
        QueueCurrentFrameRefresh(immediate: true);
    }

    private void WireAdjustmentValueTextBox(TextBox textBox, TrackBar trackBar, bool allowNegative)
    {
        textBox.Tag = new AdjustmentTextBoxBinding(trackBar, allowNegative);
        textBox.Leave += AdjustmentValueTextBox_Commit;
        textBox.KeyDown += AdjustmentValueTextBox_KeyDown;
    }

    private void AdjustmentValueTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (e.KeyCode == Keys.Enter)
        {
            CommitAdjustmentValueTextBox(textBox);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            UpdateAdjustmentValueLabels();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void AdjustmentValueTextBox_Commit(object? sender, EventArgs e)
    {
        if (sender is TextBox textBox)
            CommitAdjustmentValueTextBox(textBox);
    }

    private void CommitAdjustmentValueTextBox(TextBox textBox)
    {
        if (textBox.Tag is not AdjustmentTextBoxBinding binding)
            return;

        if (!TryParseAdjustmentText(textBox.Text, binding.AllowNegative, out int value))
        {
            UpdateAdjustmentValueLabels();
            return;
        }

        value = Math.Clamp(value, binding.TrackBar.Minimum, binding.TrackBar.Maximum);
        binding.TrackBar.Value = value;
        _project.Adjustments = ReadAdjustmentsFromControls();
        QueueCurrentFrameRefresh(immediate: true);
    }

    private void AdjustmentPreviewTimer_Tick(object? sender, EventArgs e)
    {
        _adjustmentPreviewTimer.Stop();
        _ = RefreshCurrentFrameBitmapAsync();
    }

    private void SyncAdjustmentControlsFromProject()
    {
        NormalizeProjectState(_project);

        _isSyncingAdjustmentControls = true;
        try
        {
            trackBrightness.Value = FloatToSignedTrackValue(_project.Adjustments.Brightness);
            trackContrast.Value = FloatToSignedTrackValue(_project.Adjustments.Contrast);
            trackSaturation.Value = FloatToSignedTrackValue(_project.Adjustments.Saturation);
            trackTemperature.Value = FloatToSignedTrackValue(_project.Adjustments.Temperature);
            trackSharpness.Value = FloatToUnsignedTrackValue(_project.Adjustments.Sharpness);
            trackHighlights.Value = FloatToSignedTrackValue(_project.Adjustments.Highlights);
            trackShadows.Value = FloatToSignedTrackValue(_project.Adjustments.Shadows);
            UpdateAdjustmentValueLabels();
        }
        finally
        {
            _isSyncingAdjustmentControls = false;
        }
    }

    private ImageAdjustmentSettings ReadAdjustmentsFromControls()
    {
        return new ImageAdjustmentSettings
        {
            Brightness = SignedTrackValueToFloat(trackBrightness.Value),
            Contrast = SignedTrackValueToFloat(trackContrast.Value),
            Saturation = SignedTrackValueToFloat(trackSaturation.Value),
            Temperature = SignedTrackValueToFloat(trackTemperature.Value),
            Sharpness = UnsignedTrackValueToFloat(trackSharpness.Value),
            Highlights = SignedTrackValueToFloat(trackHighlights.Value),
            Shadows = SignedTrackValueToFloat(trackShadows.Value)
        };
    }

    private void UpdateAdjustmentValueLabels()
    {
        txtBrightnessValue.Text = FormatSignedAdjustmentValue(trackBrightness.Value);
        txtContrastValue.Text = FormatSignedAdjustmentValue(trackContrast.Value);
        txtSaturationValue.Text = FormatSignedAdjustmentValue(trackSaturation.Value);
        txtTemperatureValue.Text = FormatSignedAdjustmentValue(trackTemperature.Value);
        txtSharpnessValue.Text = FormatUnsignedAdjustmentValue(trackSharpness.Value);
        txtHighlightsValue.Text = FormatSignedAdjustmentValue(trackHighlights.Value);
        txtShadowsValue.Text = FormatSignedAdjustmentValue(trackShadows.Value);
    }

    private void QueueCurrentFrameRefresh(bool immediate)
    {
        if (_currentIndex < 0 || _currentIndex >= _project.Frames.Count)
            return;

        if (immediate)
        {
            _adjustmentPreviewTimer.Stop();
            _ = RefreshCurrentFrameBitmapAsync();
            return;
        }

        _adjustmentPreviewTimer.Stop();
        _adjustmentPreviewTimer.Start();
    }

    private async Task RefreshCurrentFrameBitmapAsync()
    {
        if (_currentIndex < 0 || _currentIndex >= _project.Frames.Count)
            return;

        int frameIndex = _currentIndex;
        string sourcePath = _project.Frames[frameIndex].SourcePath;
        NormalizeProjectState(_project);
        ImageAdjustmentSettings adjustments = _project.Adjustments.Clone();
        int requestId = Interlocked.Increment(ref _framePreviewRequestId);

        var cts = new CancellationTokenSource();
        CancellationTokenSource? previous = Interlocked.Exchange(ref _framePreviewCts, cts);
        previous?.Cancel();
        previous?.Dispose();

        try
        {
            Bitmap bitmap = await Task.Run(() => LoadDisplayBitmap(sourcePath, adjustments), cts.Token);

            if (cts.IsCancellationRequested ||
                requestId != Volatile.Read(ref _framePreviewRequestId) ||
                frameIndex != _currentIndex)
            {
                bitmap.Dispose();
                return;
            }

            SDImage? previousBitmap = _currentBitmap;
            _currentBitmap = bitmap;
            previousBitmap?.Dispose();
            pictureBoxFrame.Invalidate();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Erreur d'aperÃ§u image.";
            MessageBox.Show(this, ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (ReferenceEquals(_framePreviewCts, cts))
                _framePreviewCts = null;

            cts.Dispose();
        }
    }

    private void CancelPendingFramePreview()
    {
        CancellationTokenSource? current = Interlocked.Exchange(ref _framePreviewCts, null);
        if (current is null)
            return;

        current.Cancel();
        current.Dispose();
    }

    private static void NormalizeProjectState(ProjectState project)
    {
        if (project.Adjustments is null)
            project.Adjustments = ImageAdjustmentSettings.Default;
    }

    private static decimal ClampToRange(decimal value, decimal minimum, decimal maximum)
    {
        if (value < minimum) return minimum;
        if (value > maximum) return maximum;
        return value;
    }

    private static int ConvertFpsToGifDelayCs(int fps)
    {
        int safeFps = Math.Max(1, fps);
        return Math.Max(1, (int)Math.Round(100d / safeFps, MidpointRounding.AwayFromZero));
    }

    private static Color GetFrameListColor(FrameInfo frame)
    {
        if (!frame.IsKept)
            return Color.Firebrick;

        if (frame.AnchorPoint.HasValue)
            return Color.ForestGreen;

        return Color.Black;
    }

    private static int FloatToSignedTrackValue(float value) => Math.Clamp((int)MathF.Round(value * 100f), -100, 100);

    private static int FloatToUnsignedTrackValue(float value) => Math.Clamp((int)MathF.Round(value * 100f), 0, 100);

    private static float SignedTrackValueToFloat(int value) => value / 100f;

    private static float UnsignedTrackValueToFloat(int value) => value / 100f;

    private static string FormatSignedAdjustmentValue(int value) => value > 0 ? $"+{value}%" : $"{value}%";

    private static string FormatUnsignedAdjustmentValue(int value) => $"{value}%";

    private static bool TryParseAdjustmentText(string? text, bool allowNegative, out int value)
    {
        string sanitized = (text ?? string.Empty).Trim().Replace("%", string.Empty);
        if (!allowNegative && sanitized.StartsWith("-", StringComparison.Ordinal))
        {
            value = 0;
            return false;
        }

        return int.TryParse(sanitized, out value);
    }

    private sealed record AdjustmentTextBoxBinding(TrackBar TrackBar, bool AllowNegative);

    private void ResetImageViewport()
    {
        _imageZoom = MinImageZoom;
        _imagePanOffset = SDPointF.Empty;
        _isImagePointerDown = false;
        _isImagePanning = false;
        pictureBoxFrame.Cursor = Cursors.Default;
        UpdateZoomButtons();
    }

    private void SetImageZoom(float requestedZoom, Point focusPoint)
    {
        if (_currentBitmap is null)
            return;

        float clampedZoom = Math.Clamp(requestedZoom, MinImageZoom, MaxImageZoom);
        if (Math.Abs(clampedZoom - _imageZoom) < 0.0001f)
        {
            UpdateZoomButtons();
            return;
        }

        SDPointF? focusImagePoint = ImageViewerMath.ClientToImage(
            focusPoint,
            pictureBoxFrame.ClientSize,
            _currentBitmap.Size,
            _imageZoom,
            _imagePanOffset);

        _imageZoom = clampedZoom;

        if (focusImagePoint.HasValue)
        {
            RectangleF centeredBounds = ImageViewerMath.GetImageDisplayBounds(
                pictureBoxFrame.ClientSize,
                _currentBitmap.Size,
                _imageZoom,
                SDPointF.Empty);

            float desiredLeft = focusPoint.X - ((focusImagePoint.Value.X / _currentBitmap.Width) * centeredBounds.Width);
            float desiredTop = focusPoint.Y - ((focusImagePoint.Value.Y / _currentBitmap.Height) * centeredBounds.Height);

            _imagePanOffset = new SDPointF(
                desiredLeft - centeredBounds.Left,
                desiredTop - centeredBounds.Top);
        }

        ClampImagePanOffset();
        UpdateZoomButtons();
        pictureBoxFrame.Invalidate();
    }

    private void ClampImagePanOffset()
    {
        if (_currentBitmap is null)
        {
            _imagePanOffset = SDPointF.Empty;
            return;
        }

        RectangleF centeredBounds = ImageViewerMath.GetImageDisplayBounds(
            pictureBoxFrame.ClientSize,
            _currentBitmap.Size,
            _imageZoom,
            SDPointF.Empty);

        float maxPanX = Math.Max(0f, (centeredBounds.Width - pictureBoxFrame.ClientSize.Width) / 2f);
        float maxPanY = Math.Max(0f, (centeredBounds.Height - pictureBoxFrame.ClientSize.Height) / 2f);

        _imagePanOffset = new SDPointF(
            Math.Clamp(_imagePanOffset.X, -maxPanX, maxPanX),
            Math.Clamp(_imagePanOffset.Y, -maxPanY, maxPanY));
    }

    private void UpdateZoomButtons()
    {
        btnZoomIn.Enabled = _currentBitmap is not null && _imageZoom < MaxImageZoom - 0.0001f;
        btnZoomOut.Enabled = _currentBitmap is not null && _imageZoom > MinImageZoom + 0.0001f;
    }

    private Point GetViewportCenter()
    {
        return new Point(pictureBoxFrame.ClientSize.Width / 2, pictureBoxFrame.ClientSize.Height / 2);
    }
}

