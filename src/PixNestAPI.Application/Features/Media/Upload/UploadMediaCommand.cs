using MediatR;
using PixNestAPI.Application.Features.Media.Dtos;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Media.Upload;

// IFormFile is an ASP.NET Core type — it must not cross into the Application layer.
// The controller extracts Stream + metadata from IFormFile before sending this command.
public record UploadMediaCommand(
    string SessionId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    MediaMetadata? Metadata
) : IRequest<MediaItemDto>;
