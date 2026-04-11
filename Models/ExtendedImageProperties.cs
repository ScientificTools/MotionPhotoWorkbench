namespace MotionPhotoWorkbench.Models;

public sealed class ExtendedImageProperties
{
    public string? FocalLength { get; set; }
    public string? ExposureTime { get; set; }
    public string? CameraModel { get; set; }
    public double? GpsLongitude { get; set; }
    public double? GpsLatitude { get; set; }
}
