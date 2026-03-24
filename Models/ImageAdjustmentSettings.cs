namespace MotionPhotoWorkbench.Models;

public sealed class ImageAdjustmentSettings
{
    public static ImageAdjustmentSettings Default => new();

    public float Brightness { get; set; }
    public float Contrast { get; set; }
    public float Saturation { get; set; }
    public float Temperature { get; set; }
    public float Sharpness { get; set; }
    public float Highlights { get; set; }
    public float Shadows { get; set; }

    public ImageAdjustmentSettings Clone()
    {
        return new ImageAdjustmentSettings
        {
            Brightness = Brightness,
            Contrast = Contrast,
            Saturation = Saturation,
            Temperature = Temperature,
            Sharpness = Sharpness,
            Highlights = Highlights,
            Shadows = Shadows
        };
    }

    public bool IsNeutral()
    {
        const float epsilon = 0.0001f;

        return MathF.Abs(Brightness) < epsilon &&
               MathF.Abs(Contrast) < epsilon &&
               MathF.Abs(Saturation) < epsilon &&
               MathF.Abs(Temperature) < epsilon &&
               MathF.Abs(Sharpness) < epsilon &&
               MathF.Abs(Highlights) < epsilon &&
               MathF.Abs(Shadows) < epsilon;
    }
}
