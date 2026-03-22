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
}
