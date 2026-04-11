using System.Drawing;

namespace MotionPhotoWorkbench.Utils;

public static class HelperGraphic
{
    public static PointF? ClientToImage(Point clientPoint, Size clientSize, Size imageSize)
    {
        RectangleF bounds = GetImageDisplayBounds(clientSize, imageSize, 1f, PointF.Empty);
        return ClientToImage(clientPoint, imageSize, bounds);
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
        return ClientToImage(clientPoint, imageSize, bounds);
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

    public static PointF ClampClientPointToImage(Point clientPoint, Size clientSize, Size imageSize, float zoom = 1f, PointF? panOffset = null)
    {
        RectangleF bounds = GetImageDisplayBounds(clientSize, imageSize, zoom, panOffset ?? PointF.Empty);
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return PointF.Empty;

        float x = Math.Clamp((clientPoint.X - bounds.Left) / bounds.Width * imageSize.Width, 0, imageSize.Width - 1);
        float y = Math.Clamp((clientPoint.Y - bounds.Top) / bounds.Height * imageSize.Height, 0, imageSize.Height - 1);
        return new PointF(x, y);
    }

    public static Rectangle ImageToClientRectangle(Rectangle imageRect, Size clientSize, Size imageSize, float zoom = 1f, PointF? panOffset = null)
    {
        RectangleF bounds = GetImageDisplayBounds(clientSize, imageSize, zoom, panOffset ?? PointF.Empty);
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return Rectangle.Empty;

        int left = (int)MathF.Round(bounds.Left + (imageRect.Left / (float)imageSize.Width * bounds.Width));
        int top = (int)MathF.Round(bounds.Top + (imageRect.Top / (float)imageSize.Height * bounds.Height));
        int right = (int)MathF.Round(bounds.Left + (imageRect.Right / (float)imageSize.Width * bounds.Width));
        int bottom = (int)MathF.Round(bounds.Top + (imageRect.Bottom / (float)imageSize.Height * bounds.Height));

        return Rectangle.FromLTRB(left, top, Math.Max(left + 1, right), Math.Max(top + 1, bottom));
    }

    public static PointF ClampPanOffset(Size clientSize, Size imageSize, float zoom, PointF panOffset)
    {
        RectangleF centeredBounds = GetImageDisplayBounds(clientSize, imageSize, zoom, PointF.Empty);
        if (centeredBounds.IsEmpty)
            return PointF.Empty;

        float maxPanX = Math.Max(0f, (centeredBounds.Width - clientSize.Width) / 2f);
        float maxPanY = Math.Max(0f, (centeredBounds.Height - clientSize.Height) / 2f);

        return new PointF(
            Math.Clamp(panOffset.X, -maxPanX, maxPanX),
            Math.Clamp(panOffset.Y, -maxPanY, maxPanY));
    }

    public static PointF GetPanOffsetForZoomFocus(Point focusPoint, Size clientSize, Size imageSize, float zoom, PointF focusImagePoint)
    {
        RectangleF centeredBounds = GetImageDisplayBounds(clientSize, imageSize, zoom, PointF.Empty);
        if (centeredBounds.IsEmpty || imageSize.Width <= 0 || imageSize.Height <= 0)
            return PointF.Empty;

        float desiredLeft = focusPoint.X - ((focusImagePoint.X / imageSize.Width) * centeredBounds.Width);
        float desiredTop = focusPoint.Y - ((focusImagePoint.Y / imageSize.Height) * centeredBounds.Height);

        return new PointF(
            desiredLeft - centeredBounds.Left,
            desiredTop - centeredBounds.Top);
    }

    public static (float CrossHalfSize, float PenWidth) GetAnchorMarkerStyle(float zoom)
    {
        float crossHalfSize = Math.Clamp(8f + ((zoom - 1f) * 3f), 8f, 28f);
        float penWidth = Math.Clamp(2f + ((zoom - 1f) * 0.35f), 2f, 5f);
        return (crossHalfSize, penWidth);
    }

    private static PointF? ClientToImage(Point clientPoint, Size imageSize, RectangleF bounds)
    {
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        float x = (clientPoint.X - bounds.Left) / bounds.Width * imageSize.Width;
        float y = (clientPoint.Y - bounds.Top) / bounds.Height * imageSize.Height;

        if (x < 0 || y < 0 || x >= imageSize.Width || y >= imageSize.Height)
            return null;

        return new PointF(x, y);
    }
}
