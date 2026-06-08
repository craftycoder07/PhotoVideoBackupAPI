namespace PixNestAPI.Domain.ValueObjects;

public class MediaMetadata
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Location { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? FileHash { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}
