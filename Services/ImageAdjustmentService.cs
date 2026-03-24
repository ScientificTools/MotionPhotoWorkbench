using MotionPhotoWorkbench.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MotionPhotoWorkbench.Services;

public sealed class ImageAdjustmentService
{
    public void ApplyAdjustments(Image<Rgba32> image, ImageAdjustmentSettings? settings)
    {
        if (settings is null || settings.IsNeutral())
            return;

        ApplyBasicAdjustments(image, settings);
        ApplyTemperature(image, settings.Temperature);
        ApplyToneRecovery(image, settings.Highlights, settings.Shadows);
        ApplySharpness(image, settings.Sharpness);
    }

    private static void ApplyBasicAdjustments(Image<Rgba32> image, ImageAdjustmentSettings settings)
    {
        image.Mutate(ctx =>
        {
            if (MathF.Abs(settings.Brightness) > 0.0001f)
                ctx.Brightness(1f + (settings.Brightness * 0.75f));

            if (MathF.Abs(settings.Contrast) > 0.0001f)
                ctx.Contrast(1f + settings.Contrast);

            if (MathF.Abs(settings.Saturation) > 0.0001f)
                ctx.Saturate(1f + settings.Saturation);
        });
    }

    private static void ApplyTemperature(Image<Rgba32> image, float temperature)
    {
        if (MathF.Abs(temperature) < 0.0001f)
            return;

        float warm = MathF.Max(0f, temperature);
        float cool = MathF.Max(0f, -temperature);
        float redBoost = 1f + (warm * 0.18f) - (cool * 0.10f);
        float greenBoost = 1f + (warm * 0.04f) - (cool * 0.02f);
        float blueBoost = 1f + (cool * 0.18f) - (warm * 0.12f);

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref Rgba32 pixel = ref row[x];
                    pixel.R = ClampToByte(pixel.R * redBoost);
                    pixel.G = ClampToByte(pixel.G * greenBoost);
                    pixel.B = ClampToByte(pixel.B * blueBoost);
                }
            }
        });
    }

    private static void ApplyToneRecovery(Image<Rgba32> image, float highlights, float shadows)
    {
        if (MathF.Abs(highlights) < 0.0001f && MathF.Abs(shadows) < 0.0001f)
            return;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref Rgba32 pixel = ref row[x];

                    float r = pixel.R / 255f;
                    float g = pixel.G / 255f;
                    float b = pixel.B / 255f;
                    float luminance = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);

                    float highlightMask = SmoothStep(0.55f, 1f, luminance);
                    float shadowMask = 1f - SmoothStep(0f, 0.45f, luminance);

                    float highlightFactor = 1f - (highlights * 0.55f * highlightMask);
                    float shadowLift = shadows * 0.45f * shadowMask;

                    r = Clamp01((r * highlightFactor) + shadowLift);
                    g = Clamp01((g * highlightFactor) + shadowLift);
                    b = Clamp01((b * highlightFactor) + shadowLift);

                    pixel.R = ClampToByte(r * 255f);
                    pixel.G = ClampToByte(g * 255f);
                    pixel.B = ClampToByte(b * 255f);
                }
            }
        });
    }

    private static void ApplySharpness(Image<Rgba32> image, float sharpness)
    {
        if (sharpness <= 0.0001f)
            return;

        float sigma = 0.6f + (sharpness * 1.4f);
        float amount = 0.4f + (sharpness * 1.1f);

        using Image<Rgba32> blurred = image.Clone(ctx => ctx.GaussianBlur(sigma));

        image.ProcessPixelRows(blurred, (sourceAccessor, blurredAccessor) =>
        {
            for (int y = 0; y < sourceAccessor.Height; y++)
            {
                Span<Rgba32> sourceRow = sourceAccessor.GetRowSpan(y);
                Span<Rgba32> blurredRow = blurredAccessor.GetRowSpan(y);

                for (int x = 0; x < sourceRow.Length; x++)
                {
                    ref Rgba32 sourcePixel = ref sourceRow[x];
                    Rgba32 blurredPixel = blurredRow[x];

                    sourcePixel.R = ClampToByte(sourcePixel.R + ((sourcePixel.R - blurredPixel.R) * amount));
                    sourcePixel.G = ClampToByte(sourcePixel.G + ((sourcePixel.G - blurredPixel.G) * amount));
                    sourcePixel.B = ClampToByte(sourcePixel.B + ((sourcePixel.B - blurredPixel.B) * amount));
                }
            }
        });
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Clamp01((value - edge0) / MathF.Max(0.0001f, edge1 - edge0));
        return t * t * (3f - (2f * t));
    }

    private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    private static byte ClampToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value), 0, 255);
}
