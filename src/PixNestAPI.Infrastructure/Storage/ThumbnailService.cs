using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Domain.Enums;
using SkiaSharp;

namespace PixNestAPI.Infrastructure.Storage;

public class ThumbnailService : IThumbnailService
{
    private readonly string _thumbnailsPath;
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(IConfiguration configuration, ILogger<ThumbnailService> logger)
    {
        _logger = logger;
        var basePath = configuration["StorageSettings:BasePath"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MediaBackup");
        _thumbnailsPath = Path.Combine(basePath, "Thumbnails");
        Directory.CreateDirectory(_thumbnailsPath);
    }

    public async Task<string> GenerateAsync(string mediaId, string serverPath, MediaType mediaType)
    {
        var thumbnailPath = Path.Combine(_thumbnailsPath, $"{mediaId}_thumb.jpg");

        if (File.Exists(thumbnailPath))
            return thumbnailPath;

        if (!File.Exists(serverPath))
            throw new FileNotFoundException($"Source file not found: {serverPath}");

        if (mediaType == MediaType.Video)
            throw new NotImplementedException("Video thumbnail generation requires FFMpeg.");

        await GeneratePhotoThumbnailAsync(mediaId, serverPath, thumbnailPath);
        return thumbnailPath;
    }

    public async Task<byte[]> GetBytesAsync(string thumbnailPath)
        => await File.ReadAllBytesAsync(thumbnailPath);

    private async Task GeneratePhotoThumbnailAsync(string mediaId, string sourcePath, string outputPath)
    {
        // Verify file is a supported image format by checking magic bytes
        var header = new byte[4];
        await using (var fs = File.OpenRead(sourcePath))
            _ = await fs.ReadAsync(header, 0, 4);

        bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        bool isPng  = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

        if (!isJpeg && !isPng)
            throw new InvalidDataException($"Unsupported image format for {mediaId}. Only JPEG and PNG are supported.");

        using var original = SKBitmap.Decode(sourcePath)
            ?? throw new InvalidDataException($"SkiaSharp could not decode image: {sourcePath}");

        const int maxSize = 300;
        var ratio = (double)maxSize / Math.Max(original.Width, original.Height);
        var newWidth  = (int)(original.Width  * ratio);
        var newHeight = (int)(original.Height * ratio);

        var imageInfo = new SKImageInfo(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        var sampling  = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        using var resized = original.Resize(imageInfo, sampling)
            ?? throw new InvalidOperationException("Failed to resize image.");

        using var image = SKImage.FromBitmap(resized);
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 85);

        using var output = File.OpenWrite(outputPath);
        data.SaveTo(output);

        _logger.LogInformation("Generated thumbnail for {MediaId} ({W}x{H})", mediaId, newWidth, newHeight);
    }
}
