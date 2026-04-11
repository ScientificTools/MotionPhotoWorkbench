using System;
using System.Globalization;
using System.IO;

using MotionPhotoWorkbench.Models;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace MotionPhotoWorkbench.Services;

public sealed class ImageMetadataService
{
    public ExtendedImageProperties ExtractExtendedProperties(string? imagePath)
    {
        var properties = new ExtendedImageProperties();
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return properties;

        try
        {
            var imageInfo = SixLabors.ImageSharp.Image.Identify(imagePath);
            ExifProfile? exifProfile = imageInfo?.Metadata.ExifProfile;
            if (exifProfile is null)
                return properties;

            properties.FocalLength = TryReadFocalLength(exifProfile);
            properties.ExposureTime = TryReadExposureTime(exifProfile);
            properties.CameraModel = TryReadString(exifProfile, ExifTag.Model);
            properties.GpsLatitude = TryReadGpsCoordinate(exifProfile, ExifTag.GPSLatitude, ExifTag.GPSLatitudeRef);
            properties.GpsLongitude = TryReadGpsCoordinate(exifProfile, ExifTag.GPSLongitude, ExifTag.GPSLongitudeRef);
        }
        catch
        {
            // Best effort only: project saving should still succeed even when metadata cannot be read.
        }

        return properties;
    }

    private static string? TryReadFocalLength(ExifProfile exifProfile)
    {
        if (!exifProfile.TryGetValue(ExifTag.FocalLength, out IExifValue<Rational>? value))
            return null;

        double focalLength = value.Value.ToDouble();
        return $"{focalLength.ToString("0.##", CultureInfo.InvariantCulture)} mm";
    }

    private static string? TryReadExposureTime(ExifProfile exifProfile)
    {
        if (!exifProfile.TryGetValue(ExifTag.ExposureTime, out IExifValue<Rational>? value))
            return null;

        Rational exposure = value.Value;
        if (exposure.Numerator == 0 || exposure.Denominator == 0)
            return null;

        if (exposure.Numerator == 1)
            return $"1/{exposure.Denominator} s";

        double seconds = exposure.ToDouble();
        return $"{seconds.ToString("0.######", CultureInfo.InvariantCulture)} s";
    }

    private static string? TryReadString(ExifProfile exifProfile, ExifTag<string> tag)
    {
        if (!exifProfile.TryGetValue(tag, out IExifValue<string>? value))
            return null;

        string? text = value.Value?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static double? TryReadGpsCoordinate(ExifProfile exifProfile, ExifTag<Rational[]> coordinateTag, ExifTag<string> referenceTag)
    {
        if (!exifProfile.TryGetValue(coordinateTag, out IExifValue<Rational[]>? coordinateValue) || coordinateValue.Value is null)
            return null;

        Rational[] values = coordinateValue.Value;
        if (values.Length < 3)
            return null;

        double decimalDegrees =
            values[0].ToDouble() +
            (values[1].ToDouble() / 60d) +
            (values[2].ToDouble() / 3600d);

        string? reference = TryReadString(exifProfile, referenceTag);
        if (string.Equals(reference, "S", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(reference, "W", StringComparison.OrdinalIgnoreCase))
        {
            decimalDegrees *= -1d;
        }

        return decimalDegrees;
    }
}
