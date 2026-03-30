using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;
using SDPoint = System.Drawing.Point;
using SDPointF = System.Drawing.PointF;
using ISImage = SixLabors.ImageSharp.Image;
using ISImageRgba32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;

namespace MotionPhotoWorkbench.Services;

public sealed class AnchorAutoDetectionService
{
    private const int PreferredPatchRadius = 12;
    private const int MinimumPatchRadius = 4;
    private const int SearchRadius = 36;
    private const float MinimumConfidence = 0.82f;

    public AutoAnchorSearchResult FindAnchor(string referenceImagePath, SDPointF referenceAnchor, string candidateImagePath)
    {
        using ISImageRgba32 referenceImage = ISImage.Load<Rgba32>(referenceImagePath);
        using ISImageRgba32 candidateImage = ISImage.Load<Rgba32>(candidateImagePath);
        return FindAnchor(referenceImage, referenceAnchor, candidateImage);
    }

    public AutoAnchorSearchResult FindAnchor(ISImageRgba32 referenceImage, SDPointF referenceAnchor, ISImageRgba32 candidateImage)
    {
        int referenceX = (int)MathF.Round(referenceAnchor.X);
        int referenceY = (int)MathF.Round(referenceAnchor.Y);

        int patchRadius = Math.Min(
            PreferredPatchRadius,
            Math.Min(
                Math.Min(referenceX, (referenceImage.Width - 1) - referenceX),
                Math.Min(referenceY, (referenceImage.Height - 1) - referenceY)));

        if (patchRadius < MinimumPatchRadius)
            return AutoAnchorSearchResult.NotFound("Reference anchor is too close to the image border.");

        int patchSize = (patchRadius * 2) + 1;
        float[] referencePatch = ExtractGrayPatch(referenceImage, referenceX - patchRadius, referenceY - patchRadius, patchSize, patchSize);

        float bestScore = float.NegativeInfinity;
        SDPoint bestPoint = new(referenceX, referenceY);

        foreach (SDPoint candidatePoint in EnumerateSpiral(referenceX, referenceY, SearchRadius))
        {
            if (!CanFitPatch(candidateImage.Width, candidateImage.Height, candidatePoint.X, candidatePoint.Y, patchRadius))
                continue;

            float[] candidatePatch = ExtractGrayPatch(candidateImage, candidatePoint.X - patchRadius, candidatePoint.Y - patchRadius, patchSize, patchSize);
            float score = ComputeZeroMeanNormalizedCrossCorrelation(referencePatch, candidatePatch);

            if (float.IsNaN(score))
                continue;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = candidatePoint;
            }
        }

        if (bestScore < MinimumConfidence)
            return AutoAnchorSearchResult.NotFound($"No reliable match found (best score {bestScore:0.000}).");

        return AutoAnchorSearchResult.Found(new SDPointF(bestPoint.X, bestPoint.Y), bestScore);
    }

    private static bool CanFitPatch(int width, int height, int centerX, int centerY, int patchRadius)
    {
        return centerX - patchRadius >= 0 &&
               centerY - patchRadius >= 0 &&
               centerX + patchRadius < width &&
               centerY + patchRadius < height;
    }

    private static float[] ExtractGrayPatch(ISImageRgba32 image, int startX, int startY, int width, int height)
    {
        float[] buffer = new float[width * height];
        int index = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(startY + y);
                for (int x = 0; x < width; x++)
                {
                    Rgba32 pixel = row[startX + x];
                    buffer[index++] = ((0.299f * pixel.R) + (0.587f * pixel.G) + (0.114f * pixel.B)) / 255f;
                }
            }
        });

        return buffer;
    }

    private static float ComputeZeroMeanNormalizedCrossCorrelation(float[] reference, float[] candidate)
    {
        if (reference.Length != candidate.Length || reference.Length == 0)
            return float.NaN;

        float referenceMean = 0f;
        float candidateMean = 0f;
        for (int i = 0; i < reference.Length; i++)
        {
            referenceMean += reference[i];
            candidateMean += candidate[i];
        }

        referenceMean /= reference.Length;
        candidateMean /= candidate.Length;

        float numerator = 0f;
        float referenceVariance = 0f;
        float candidateVariance = 0f;

        for (int i = 0; i < reference.Length; i++)
        {
            float referenceDelta = reference[i] - referenceMean;
            float candidateDelta = candidate[i] - candidateMean;
            numerator += referenceDelta * candidateDelta;
            referenceVariance += referenceDelta * referenceDelta;
            candidateVariance += candidateDelta * candidateDelta;
        }

        if (referenceVariance <= 0.000001f || candidateVariance <= 0.000001f)
            return float.NaN;

        return numerator / MathF.Sqrt(referenceVariance * candidateVariance);
    }

    private static IEnumerable<SDPoint> EnumerateSpiral(int centerX, int centerY, int maxRadius)
    {
        yield return new SDPoint(centerX, centerY);

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            int left = centerX - radius;
            int right = centerX + radius;
            int top = centerY - radius;
            int bottom = centerY + radius;

            for (int x = left; x <= right; x++)
                yield return new SDPoint(x, top);

            for (int y = top + 1; y <= bottom; y++)
                yield return new SDPoint(right, y);

            for (int x = right - 1; x >= left; x--)
                yield return new SDPoint(x, bottom);

            for (int y = bottom - 1; y > top; y--)
                yield return new SDPoint(left, y);
        }
    }
}

public sealed record AutoAnchorSearchResult(bool Success, SDPointF? AnchorPoint, float Score, string? FailureReason)
{
    public static AutoAnchorSearchResult Found(SDPointF anchorPoint, float score) => new(true, anchorPoint, score, null);

    public static AutoAnchorSearchResult NotFound(string reason) => new(false, null, float.NaN, reason);
}
