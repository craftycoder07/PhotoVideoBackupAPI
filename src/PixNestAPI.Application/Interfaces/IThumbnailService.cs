using PixNestAPI.Domain.Enums;

namespace PixNestAPI.Application.Interfaces;

public interface IThumbnailService
{
    Task<string> GenerateAsync(string mediaId, string serverPath, MediaType mediaType);
    Task<byte[]> GetBytesAsync(string thumbnailPath);
}
