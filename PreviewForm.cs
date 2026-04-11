using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

using MotionPhotoWorkbench.Utils;

namespace MotionPhotoWorkbench;

public sealed class PreviewForm : Form
{
    private enum SelectionInteractionMode
    {
        None,
        Draw,
        Move,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight
    }

    public enum PreviewExportChoice
    {
        None,
        Gif,
        Mpeg,
        WebM,
        WebP
    }

    private sealed class CropRatioOption
    {
        public string Label { get; }
        public float? AspectRatio { get; }

        public CropRatioOption(string label, float? aspectRatio)
        {
            Label = label;
            AspectRatio = aspectRatio;
        }

        public override string ToString() => Label;
    }

    private readonly PictureBox _pictureBox;
    private readonly Button _btnExportGif;
    private readonly Button _btnExportMpeg;
    private readonly Button _btnExportWebM;
    private readonly Button _btnExportWebP;
    private readonly Label _lblSelection;
    private readonly Label _lblOperationStatus;
    private readonly ComboBox _cmbAspectRatio;
    private readonly NumericUpDown _numFps;
    private readonly ProgressBar _progressBusy;
    private readonly Rectangle _imageBounds;
    private readonly List<CropRatioOption> _ratioOptions;

    private bool _isPointerDown;
    private bool _isDragging;
    private Point _selectionStart;
    private Rectangle _selectionInImage;
    private Rectangle _selectionAtPointerDown;
    private Point _pointerDownImagePoint;
    private SelectionInteractionMode _interactionMode;

    private const int HandleHitSize = 12;
    private const int DragThreshold = 3;

    public Func<PreviewForm, PreviewExportChoice, Task>? ExportRequestedAsync { get; set; }

    public PreviewForm(Image previewImage, Rectangle cropArea, int initialFps, Rectangle? initialSelection = null)
    {
        Text = "Automatic crop preview";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 580);
        ApplyInitialWindowSize();

        _imageBounds = new Rectangle(0, 0, previewImage.Width, previewImage.Height);
        float originalAspectRatio = cropArea.Height > 0
            ? (float)cropArea.Width / cropArea.Height
            : (float)previewImage.Width / Math.Max(1, previewImage.Height);

        _ratioOptions = new List<CropRatioOption>
        {
            new("Match original", originalAspectRatio),
            new("Free", null),
            new("Square (1:1)", 1f),
            new("16:9", 16f / 9f),
            new("5:4", 5f / 4f),
            new("4:3", 4f / 3f),
            new("2:1", 2f),
            new("9:16", 9f / 16f),
            new("4:5", 4f / 5f),
            new("3:4", 3f / 4f),
            new("1:2", 0.5f)
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var lblInfo = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Text = $"Selected intersection: X={cropArea.X}, Y={cropArea.Y}, W={cropArea.Width}, H={cropArea.Height}"
        };
        root.Controls.Add(lblInfo, 0, 0);

        var ratioPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 6, 0, 0)
        };
        var lblAspectRatio = new Label
        {
            AutoSize = true,
            Text = "Crop aspect ratio:",
            Margin = new Padding(0, 8, 8, 0),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };
        ratioPanel.Controls.Add(lblAspectRatio);

        _cmbAspectRatio = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 230,
            Margin = new Padding(0, 2, 0, 0),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.Gainsboro
        };
        _cmbAspectRatio.Items.AddRange(_ratioOptions.ToArray());
        _cmbAspectRatio.SelectedIndexChanged += CmbAspectRatio_SelectedIndexChanged;
        ratioPanel.Controls.Add(_cmbAspectRatio);

        _numFps = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 120,
            Value = Math.Clamp(initialFps, 1, 120),
            Width = 70,
            Margin = new Padding(0, 2, 0, 0)
        };
        root.Controls.Add(ratioPanel, 0, 1);

        _lblSelection = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        root.Controls.Add(_lblSelection, 0, 2);

        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.Black,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = previewImage
        };
        _pictureBox.MouseDown += PictureBox_MouseDown;
        _pictureBox.MouseMove += PictureBox_MouseMove;
        _pictureBox.MouseUp += PictureBox_MouseUp;
        _pictureBox.MouseLeave += PictureBox_MouseLeave;
        _pictureBox.Paint += PictureBox_Paint;
        root.Controls.Add(_pictureBox, 0, 3);

        var operationPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 8, 0, 0)
        };
        operationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        operationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(operationPanel, 0, 4);

        _lblOperationStatus = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Ready.",
            TextAlign = ContentAlignment.TopLeft,
            Margin = new Padding(0, 6, 8, 0),
            MinimumSize = new Size(0, 40),
            Padding = new Padding(0, 2, 0, 2)
        };
        operationPanel.Controls.Add(_lblOperationStatus, 0, 0);

        _progressBusy = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Width = 180,
            Visible = false,
            Margin = new Padding(0, 2, 0, 0)
        };
        operationPanel.Controls.Add(_progressBusy, 1, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true
        };
        root.Controls.Add(buttons, 0, 5);

        var fpsLabel = new Label
        {
            AutoSize = true,
            Text = "Export FPS:",
            Margin = new Padding(0, 8, 8, 0),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        _btnExportGif = new Button
        {
            Text = "Export GIF",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.Gainsboro,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _btnExportGif.Click += async (_, _) => await RequestExportAsync(PreviewExportChoice.Gif);

        _btnExportMpeg = new Button
        {
            Text = "Export MPEG",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.Gainsboro,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _btnExportMpeg.Click += async (_, _) => await RequestExportAsync(PreviewExportChoice.Mpeg);

        _btnExportWebM = new Button
        {
            Text = "Export WebM",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.Gainsboro,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _btnExportWebM.Click += async (_, _) => await RequestExportAsync(PreviewExportChoice.WebM);

        _btnExportWebP = new Button
        {
            Text = "Export WebP",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.Gainsboro,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        _btnExportWebP.Click += async (_, _) => await RequestExportAsync(PreviewExportChoice.WebP);

        buttons.Controls.Add(fpsLabel);
        buttons.Controls.Add(_numFps);
        buttons.Controls.Add(_btnExportMpeg);
        buttons.Controls.Add(_btnExportWebM);
        buttons.Controls.Add(_btnExportWebP);
        buttons.Controls.Add(_btnExportGif);

        AcceptButton = _btnExportGif;

        bool hasInitialSelection = initialSelection.HasValue;
        Rectangle startingSelection = initialSelection ?? _imageBounds;
        _selectionInImage = hasInitialSelection
            ? Rectangle.Intersect(EnsureMinimumSelection(startingSelection), _imageBounds)
            : _imageBounds;

        if (_cmbAspectRatio.Items.Count > 0)
            _cmbAspectRatio.SelectedIndex = hasInitialSelection ? 1 : 0;
        _selectionInImage = FitSelectionToAspectRatio(_selectionInImage, SelectedAspectRatio);
        UpdateSelectionLabel();
    }

    public Rectangle SelectedCrop => _selectionInImage;
    public PreviewExportChoice ExportChoice { get; private set; }
    public int ExportFps => (int)_numFps.Value;

    public void BeginBusy(string message)
    {
        _lblOperationStatus.Text = message;
        _progressBusy.Visible = true;
        UseWaitCursor = true;
        SetInteractiveState(false);
    }

    public void UpdateBusyMessage(string message)
    {
        _lblOperationStatus.Text = message;
    }

    public void EndBusy(string message = "Ready.")
    {
        _lblOperationStatus.Text = message;
        _progressBusy.Visible = false;
        UseWaitCursor = false;
        SetInteractiveState(true);
    }

    private float? SelectedAspectRatio => (_cmbAspectRatio.SelectedItem as CropRatioOption)?.AspectRatio;

    private void ApplyInitialWindowSize()
    {
        Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        int width = Math.Max(MinimumSize.Width, (int)Math.Round(workingArea.Width * 0.75));
        int height = Math.Max(MinimumSize.Height, (int)Math.Round(workingArea.Height * 0.75));

        Size = new Size(
            Math.Min(width, workingArea.Width),
            Math.Min(height, workingArea.Height));
    }

    private void SetInteractiveState(bool enabled)
    {
        _cmbAspectRatio.Enabled = enabled;
        _numFps.Enabled = enabled;
        _btnExportGif.Enabled = enabled;
        _btnExportMpeg.Enabled = enabled;
        _btnExportWebM.Enabled = enabled;
        _btnExportWebP.Enabled = enabled;
        _pictureBox.Enabled = enabled;
    }

    private async Task RequestExportAsync(PreviewExportChoice choice)
    {
        ExportChoice = choice;

        if (ExportRequestedAsync is null)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        try
        {
            await ExportRequestedAsync(this, choice);
        }
        catch
        {
            EndBusy("Ready.");
            throw;
        }
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _pictureBox.Image is null)
        {
            if (e.Button == MouseButtons.Right)
            {
                _isPointerDown = false;
                _isDragging = false;
                _interactionMode = SelectionInteractionMode.None;
                _selectionInImage = FitSelectionToAspectRatio(_imageBounds, SelectedAspectRatio);
                _pictureBox.Invalidate();
                UpdateSelectionLabel();
            }

            return;
        }

        var imagePoint = HelperGraphic.ClientToImage(e.Location, _pictureBox.ClientSize, _pictureBox.Image.Size);
        if (!imagePoint.HasValue)
            return;

        Point imagePointRounded = Point.Round(imagePoint.Value);
        _pointerDownImagePoint = imagePointRounded;
        _selectionAtPointerDown = _selectionInImage;
        _isPointerDown = true;
        _isDragging = false;
        _interactionMode = HitTestInteractionMode(e.Location, imagePointRounded);
        _selectionStart = GetAnchorPointForResize(_interactionMode, _selectionAtPointerDown, imagePointRounded);
        _pictureBox.Capture = true;
    }

    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_pictureBox.Image is null)
            return;

        var imagePoint = HelperGraphic.ClientToImage(e.Location, _pictureBox.ClientSize, _pictureBox.Image.Size)
            ?? ClampClientPointToImage(e.Location);
        Point imagePointRounded = Point.Round(imagePoint);

        if (!_isPointerDown)
        {
            UpdatePointerCursor(e.Location, imagePointRounded);
            return;
        }

        if (!_isDragging)
        {
            int dx = imagePointRounded.X - _pointerDownImagePoint.X;
            int dy = imagePointRounded.Y - _pointerDownImagePoint.Y;
            if (Math.Abs(dx) < DragThreshold && Math.Abs(dy) < DragThreshold)
                return;

            _isDragging = true;
        }

        _selectionInImage = _interactionMode switch
        {
            SelectionInteractionMode.Move => MoveSelection(_selectionAtPointerDown, imagePointRounded.X - _pointerDownImagePoint.X, imagePointRounded.Y - _pointerDownImagePoint.Y),
            SelectionInteractionMode.ResizeTopLeft or
            SelectionInteractionMode.ResizeTopRight or
            SelectionInteractionMode.ResizeBottomLeft or
            SelectionInteractionMode.ResizeBottomRight or
            SelectionInteractionMode.Draw => BuildSelection(_selectionStart, imagePointRounded, SelectedAspectRatio),
            _ => _selectionInImage
        };

        _pictureBox.Invalidate();
        UpdateSelectionLabel();
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isPointerDown)
            return;

        _isPointerDown = false;
        _isDragging = false;
        _pictureBox.Capture = false;
        _interactionMode = SelectionInteractionMode.None;
        _selectionInImage = FitSelectionToAspectRatio(EnsureMinimumSelection(_selectionInImage), SelectedAspectRatio);
        UpdatePointerCursor(e.Location, Point.Round(ClampClientPointToImage(e.Location)));
        _pictureBox.Invalidate();
        UpdateSelectionLabel();
    }

    private void PictureBox_MouseLeave(object? sender, EventArgs e)
    {
        if (!_isPointerDown)
            _pictureBox.Cursor = Cursors.Default;
    }

    private void PictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_pictureBox.Image is null)
            return;

        Rectangle clientRect = ImageToClientRectangle(_selectionInImage);
        using var fillBrush = new SolidBrush(Color.FromArgb(50, Color.Lime));
        using var outlinePen = new Pen(Color.Lime, 2);

        e.Graphics.FillRectangle(fillBrush, clientRect);
        e.Graphics.DrawRectangle(outlinePen, clientRect);

        foreach (Rectangle handle in GetClientHandleRectangles(clientRect))
        {
            e.Graphics.FillRectangle(Brushes.White, handle);
            e.Graphics.DrawRectangle(Pens.Lime, handle);
        }
    }

    private void CmbAspectRatio_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectionInImage = FitSelectionToAspectRatio(_selectionInImage, SelectedAspectRatio);
        _pictureBox.Invalidate();
        UpdateSelectionLabel();
    }

    private PointF ClampClientPointToImage(Point clientPoint)
    {
        if (_pictureBox.Image is null)
            return PointF.Empty;

        return HelperGraphic.ClampClientPointToImage(clientPoint, _pictureBox.ClientSize, _pictureBox.Image.Size);
    }

    private Rectangle ImageToClientRectangle(Rectangle imageRect)
    {
        if (_pictureBox.Image is null)
            return Rectangle.Empty;

        return HelperGraphic.ImageToClientRectangle(imageRect, _pictureBox.ClientSize, _pictureBox.Image.Size);
    }

    private Rectangle EnsureMinimumSelection(Rectangle rectangle)
    {
        int width = Math.Max(1, rectangle.Width);
        int height = Math.Max(1, rectangle.Height);

        int x = Math.Clamp(rectangle.X, 0, Math.Max(0, _imageBounds.Width - width));
        int y = Math.Clamp(rectangle.Y, 0, Math.Max(0, _imageBounds.Height - height));

        return Rectangle.Intersect(new Rectangle(x, y, width, height), _imageBounds);
    }

    private SelectionInteractionMode HitTestInteractionMode(Point clientPoint, Point imagePoint)
    {
        Rectangle clientRect = ImageToClientRectangle(_selectionInImage);
        if (!clientRect.IsEmpty)
        {
            Rectangle[] handles = GetClientHandleRectangles(clientRect);
            if (handles[0].Contains(clientPoint)) return SelectionInteractionMode.ResizeTopLeft;
            if (handles[1].Contains(clientPoint)) return SelectionInteractionMode.ResizeTopRight;
            if (handles[2].Contains(clientPoint)) return SelectionInteractionMode.ResizeBottomLeft;
            if (handles[3].Contains(clientPoint)) return SelectionInteractionMode.ResizeBottomRight;
            if (_selectionInImage.Contains(imagePoint)) return SelectionInteractionMode.Move;
        }

        return SelectionInteractionMode.Draw;
    }

    private void UpdatePointerCursor(Point clientPoint, Point imagePoint)
    {
        SelectionInteractionMode mode = HitTestInteractionMode(clientPoint, imagePoint);
        _pictureBox.Cursor = mode switch
        {
            SelectionInteractionMode.Move => Cursors.SizeAll,
            SelectionInteractionMode.ResizeTopLeft => Cursors.SizeNWSE,
            SelectionInteractionMode.ResizeBottomRight => Cursors.SizeNWSE,
            SelectionInteractionMode.ResizeTopRight => Cursors.SizeNESW,
            SelectionInteractionMode.ResizeBottomLeft => Cursors.SizeNESW,
            _ => Cursors.Cross
        };
    }

    private static Rectangle[] GetClientHandleRectangles(Rectangle clientRect)
    {
        int half = HandleHitSize / 2;
        return new[]
        {
            new Rectangle(clientRect.Left - half, clientRect.Top - half, HandleHitSize, HandleHitSize),
            new Rectangle(clientRect.Right - half, clientRect.Top - half, HandleHitSize, HandleHitSize),
            new Rectangle(clientRect.Left - half, clientRect.Bottom - half, HandleHitSize, HandleHitSize),
            new Rectangle(clientRect.Right - half, clientRect.Bottom - half, HandleHitSize, HandleHitSize)
        };
    }

    private static Point GetAnchorPointForResize(SelectionInteractionMode mode, Rectangle selection, Point fallback)
    {
        return mode switch
        {
            SelectionInteractionMode.ResizeTopLeft => new Point(selection.Right, selection.Bottom),
            SelectionInteractionMode.ResizeTopRight => new Point(selection.Left, selection.Bottom),
            SelectionInteractionMode.ResizeBottomLeft => new Point(selection.Right, selection.Top),
            SelectionInteractionMode.ResizeBottomRight => new Point(selection.Left, selection.Top),
            _ => fallback
        };
    }

    private Rectangle MoveSelection(Rectangle selection, int deltaX, int deltaY)
    {
        int x = Math.Clamp(selection.X + deltaX, _imageBounds.Left, _imageBounds.Right - selection.Width);
        int y = Math.Clamp(selection.Y + deltaY, _imageBounds.Top, _imageBounds.Bottom - selection.Height);
        return new Rectangle(x, y, selection.Width, selection.Height);
    }

    private Rectangle BuildSelection(Point start, Point end, float? aspectRatio)
    {
        if (!aspectRatio.HasValue || aspectRatio.Value <= 0f)
            return NormalizeRectangle(start, end);

        int horizontalDirection = end.X >= start.X ? 1 : -1;
        int verticalDirection = end.Y >= start.Y ? 1 : -1;

        float rawWidth = Math.Abs(end.X - start.X);
        float rawHeight = Math.Abs(end.Y - start.Y);
        float ratio = aspectRatio.Value;

        float width;
        float height;

        if (rawWidth < 1f && rawHeight < 1f)
        {
            width = 1f;
            height = 1f / ratio;
        }
        else if (rawHeight < 1f)
        {
            width = Math.Max(1f, rawWidth);
            height = Math.Max(1f, width / ratio);
        }
        else if ((rawWidth / rawHeight) > ratio)
        {
            height = Math.Max(1f, rawHeight);
            width = Math.Max(1f, height * ratio);
        }
        else
        {
            width = Math.Max(1f, rawWidth);
            height = Math.Max(1f, width / ratio);
        }

        float maxWidth = horizontalDirection > 0
            ? _imageBounds.Right - start.X
            : start.X - _imageBounds.Left;
        float maxHeight = verticalDirection > 0
            ? _imageBounds.Bottom - start.Y
            : start.Y - _imageBounds.Top;

        maxWidth = Math.Max(1f, maxWidth);
        maxHeight = Math.Max(1f, maxHeight);

        if (width > maxWidth || height > maxHeight)
        {
            if ((maxWidth / maxHeight) > ratio)
            {
                height = maxHeight;
                width = Math.Max(1f, height * ratio);
            }
            else
            {
                width = maxWidth;
                height = Math.Max(1f, width / ratio);
            }
        }

        int finalWidth = Math.Max(1, (int)MathF.Round(width));
        int finalHeight = Math.Max(1, (int)MathF.Round(height));

        int left = horizontalDirection > 0 ? start.X : start.X - finalWidth;
        int top = verticalDirection > 0 ? start.Y : start.Y - finalHeight;

        return Rectangle.Intersect(new Rectangle(left, top, finalWidth, finalHeight), _imageBounds);
    }

    private Rectangle FitSelectionToAspectRatio(Rectangle selection, float? aspectRatio)
    {
        Rectangle bounded = Rectangle.Intersect(EnsureMinimumSelection(selection), _imageBounds);

        if (!aspectRatio.HasValue || aspectRatio.Value <= 0f)
            return bounded;

        float ratio = aspectRatio.Value;
        float centerX = bounded.Left + (bounded.Width / 2f);
        float centerY = bounded.Top + (bounded.Height / 2f);

        float width = bounded.Width;
        float height = bounded.Height;

        if ((width / height) > ratio)
            width = height * ratio;
        else
            height = width / ratio;

        width = Math.Max(1f, width);
        height = Math.Max(1f, height);

        int finalWidth = Math.Max(1, (int)MathF.Round(width));
        int finalHeight = Math.Max(1, (int)MathF.Round(height));
        int x = (int)MathF.Round(centerX - (finalWidth / 2f));
        int y = (int)MathF.Round(centerY - (finalHeight / 2f));

        x = Math.Clamp(x, _imageBounds.Left, Math.Max(_imageBounds.Left, _imageBounds.Right - finalWidth));
        y = Math.Clamp(y, _imageBounds.Top, Math.Max(_imageBounds.Top, _imageBounds.Bottom - finalHeight));

        Rectangle adjusted = new(x, y, finalWidth, finalHeight);
        return Rectangle.Intersect(adjusted, _imageBounds);
    }

    private static Rectangle NormalizeRectangle(Point start, Point end)
    {
        int left = Math.Min(start.X, end.X);
        int top = Math.Min(start.Y, end.Y);
        int right = Math.Max(start.X, end.X);
        int bottom = Math.Max(start.Y, end.Y);

        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private void UpdateSelectionLabel()
    {
        string ratioLabel = (_cmbAspectRatio.SelectedItem as CropRatioOption)?.Label ?? "Free";
        _lblSelection.Text =
            $"Additional crop: X={_selectionInImage.X}, Y={_selectionInImage.Y}, W={_selectionInImage.Width}, H={_selectionInImage.Height} | ratio: {ratioLabel} | right click = reset";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pictureBox.Image?.Dispose();

        base.Dispose(disposing);
    }
}
