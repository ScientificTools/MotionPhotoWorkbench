using System.Drawing;

namespace MotionPhotoWorkbench.Utils;

public static class ImageViewerMath
{
    public static PointF? ClientToImage(Point clientPoint, Size clientSize, Size imageSize)
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

        float x = (clientPoint.X - offsetX) / scale;
        float y = (clientPoint.Y - offsetY) / scale;

        if (x < 0 || y < 0 || x >= imageSize.Width || y >= imageSize.Height)
            return null;

        return new PointF(x, y);
    }

    public static RectangleF GetImageDisplayBounds(Size clientSize, Size imageSize, float zoom, PointF panOffset)
    {
        if (clientSize.Width <= 0 || clientSize.Height <= 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
            return RectangleF.Empty;

        float ratioX = (float)clientSize.Width / imageSize.Width;
        float ratioY = (float)clientSize.Height / imageSize.Height;
        float baseScale = Math.Min(ratioX, ratioY);
        float scale = baseScale * zoom;

        float displayedWidth = imageSize.Width * scale;
        float displayedHeight = imageSize.Height * scale;

        float offsetX = (clientSize.Width - displayedWidth) / 2f + panOffset.X;
        float offsetY = (clientSize.Height - displayedHeight) / 2f + panOffset.Y;

        return new RectangleF(offsetX, offsetY, displayedWidth, displayedHeight);
    }

    public static PointF? ClientToImage(Point clientPoint, Size clientSize, Size imageSize, float zoom, PointF panOffset)
    {
        RectangleF bounds = GetImageDisplayBounds(clientSize, imageSize, zoom, panOffset);
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        float x = (clientPoint.X - bounds.Left) / bounds.Width * imageSize.Width;
        float y = (clientPoint.Y - bounds.Top) / bounds.Height * imageSize.Height;

        if (x < 0 || y < 0 || x >= imageSize.Width || y >= imageSize.Height)
            return null;

        return new PointF(x, y);
    }

    public static PointF? ImageToClient(Size clientSize, Size imageSize, PointF imagePoint, float zoom, PointF panOffset)
    {
        RectangleF bounds = GetImageDisplayBounds(clientSize, imageSize, zoom, panOffset);
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        return new PointF(
            bounds.Left + (imagePoint.X / imageSize.Width * bounds.Width),
            bounds.Top + (imagePoint.Y / imageSize.Height * bounds.Height));
    }
}
