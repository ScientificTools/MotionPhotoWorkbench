using System;
using System.Drawing;
using System.Windows.Forms;

using MotionPhotoWorkbench.Utils;

namespace MotionPhotoWorkbench;

public sealed class PreviewForm : Form
{
    public enum PreviewExportChoice
    {
        None,
        Gif,
        Mpeg
    }

    private readonly PictureBox _pictureBox;
    private readonly Button _btnExportGif;
    private readonly Button _btnExportMpeg;
    private readonly Button _btnClose;
    private readonly Label _lblSelection;
    private readonly Rectangle _imageBounds;

    private bool _isSelecting;
    private Point _selectionStart;
    private Rectangle _selectionInImage;

    public PreviewForm(Image previewImage, Rectangle cropArea, Rectangle? initialSelection = null)
    {
        Text = "Apercu du crop automatique";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(700, 550);
        Width = 1000;
        Height = 750;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var lblInfo = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Text = $"Intersection retenue : X={cropArea.X}, Y={cropArea.Y}, W={cropArea.Width}, H={cropArea.Height}"
        };
        root.Controls.Add(lblInfo, 0, 0);

        _lblSelection = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        root.Controls.Add(_lblSelection, 0, 1);

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
        _pictureBox.Paint += PictureBox_Paint;
        root.Controls.Add(_pictureBox, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false
        };
        root.Controls.Add(buttons, 0, 3);

        _btnExportGif = new Button
        {
            Text = "Exporter en GIF",
            AutoSize = true
        };
        _btnExportGif.Click += (_, _) => SelectExportChoice(PreviewExportChoice.Gif);

        _btnExportMpeg = new Button
        {
            Text = "Exporter en MPEG",
            AutoSize = true
        };
        _btnExportMpeg.Click += (_, _) => SelectExportChoice(PreviewExportChoice.Mpeg);

        _btnClose = new Button
        {
            Text = "Fermer",
            AutoSize = true
        };
        _btnClose.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttons.Controls.Add(_btnExportGif);
        buttons.Controls.Add(_btnExportMpeg);
        buttons.Controls.Add(_btnClose);

        AcceptButton = _btnExportGif;
        CancelButton = _btnClose;

        _imageBounds = new Rectangle(0, 0, previewImage.Width, previewImage.Height);
        _selectionInImage = initialSelection.HasValue
            ? Rectangle.Intersect(EnsureMinimumSelection(initialSelection.Value), _imageBounds)
            : _imageBounds;
        UpdateSelectionLabel();
    }

    public Rectangle SelectedCrop => _selectionInImage;
    public PreviewExportChoice ExportChoice { get; private set; }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _pictureBox.Image is null)
        {
            if (e.Button == MouseButtons.Right)
            {
                _isSelecting = false;
                _selectionInImage = _imageBounds;
                _pictureBox.Invalidate();
                UpdateSelectionLabel();
            }

            return;
        }

        var imagePoint = ImageViewerMath.ClientToImage(e.Location, _pictureBox.ClientSize, _pictureBox.Image.Size);
        if (!imagePoint.HasValue)
            return;

        _isSelecting = true;
        _selectionStart = Point.Round(imagePoint.Value);
        _selectionInImage = NormalizeRectangle(_selectionStart, _selectionStart);
        _pictureBox.Invalidate();
        UpdateSelectionLabel();
    }

    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isSelecting || _pictureBox.Image is null)
            return;

        var imagePoint = ImageViewerMath.ClientToImage(e.Location, _pictureBox.ClientSize, _pictureBox.Image.Size)
            ?? ClampClientPointToImage(e.Location);

        _selectionInImage = NormalizeRectangle(_selectionStart, Point.Round(imagePoint));
        _pictureBox.Invalidate();
        UpdateSelectionLabel();
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isSelecting)
            return;

        _isSelecting = false;
        _selectionInImage = EnsureMinimumSelection(_selectionInImage);
        _pictureBox.Invalidate();
        UpdateSelectionLabel();
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
    }

    private PointF ClampClientPointToImage(Point clientPoint)
    {
        if (_pictureBox.Image is null)
            return PointF.Empty;

        Size imageSize = _pictureBox.Image.Size;
        Size clientSize = _pictureBox.ClientSize;
        float ratioX = (float)clientSize.Width / imageSize.Width;
        float ratioY = (float)clientSize.Height / imageSize.Height;
        float scale = Math.Min(ratioX, ratioY);
        float displayedWidth = imageSize.Width * scale;
        float displayedHeight = imageSize.Height * scale;
        float offsetX = (clientSize.Width - displayedWidth) / 2f;
        float offsetY = (clientSize.Height - displayedHeight) / 2f;

        float x = Math.Clamp((clientPoint.X - offsetX) / scale, 0, imageSize.Width - 1);
        float y = Math.Clamp((clientPoint.Y - offsetY) / scale, 0, imageSize.Height - 1);
        return new PointF(x, y);
    }

    private Rectangle ImageToClientRectangle(Rectangle imageRect)
    {
        if (_pictureBox.Image is null)
            return Rectangle.Empty;

        Size imageSize = _pictureBox.Image.Size;
        Size clientSize = _pictureBox.ClientSize;
        float ratioX = (float)clientSize.Width / imageSize.Width;
        float ratioY = (float)clientSize.Height / imageSize.Height;
        float scale = Math.Min(ratioX, ratioY);
        float displayedWidth = imageSize.Width * scale;
        float displayedHeight = imageSize.Height * scale;
        float offsetX = (clientSize.Width - displayedWidth) / 2f;
        float offsetY = (clientSize.Height - displayedHeight) / 2f;

        int left = (int)MathF.Round(offsetX + (imageRect.Left * scale));
        int top = (int)MathF.Round(offsetY + (imageRect.Top * scale));
        int right = (int)MathF.Round(offsetX + (imageRect.Right * scale));
        int bottom = (int)MathF.Round(offsetY + (imageRect.Bottom * scale));

        return Rectangle.FromLTRB(left, top, Math.Max(left + 1, right), Math.Max(top + 1, bottom));
    }

    private Rectangle EnsureMinimumSelection(Rectangle rectangle)
    {
        int width = Math.Max(1, rectangle.Width);
        int height = Math.Max(1, rectangle.Height);

        int x = Math.Clamp(rectangle.X, 0, Math.Max(0, _imageBounds.Width - width));
        int y = Math.Clamp(rectangle.Y, 0, Math.Max(0, _imageBounds.Height - height));

        return Rectangle.Intersect(new Rectangle(x, y, width, height), _imageBounds);
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
        _lblSelection.Text =
            $"Selection additionnelle : X={_selectionInImage.X}, Y={_selectionInImage.Y}, W={_selectionInImage.Width}, H={_selectionInImage.Height} | clic droit = image complete";
    }

    private void SelectExportChoice(PreviewExportChoice choice)
    {
        ExportChoice = choice;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pictureBox.Image?.Dispose();

        base.Dispose(disposing);
    }
}
