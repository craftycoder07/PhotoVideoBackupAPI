using MediatR;
using PixNestAPI.Application.Features.Media.Dtos;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Features.Media.GetItem;

public class GetMediaItemHandler : IRequestHandler<GetMediaItemQuery, MediaItemDto>
{
    private readonly IMediaRepository _media;

    public GetMediaItemHandler(IMediaRepository media) => _media = media;

    public async Task<MediaItemDto> Handle(GetMediaItemQuery request, CancellationToken ct)
    {
        var item = await _media.GetByIdAsync(request.MediaId, ct)
            ?? throw new MediaItemNotFoundException(request.MediaId);

        return new MediaItemDto
        {
            Id = item.Id,
            SessionId = item.SessionId,
            FileName = item.FileName,
            FileSize = item.FileSize,
            FileExtension = item.FileExtension,
            Type = item.Type,
            Status = item.Status,
            CreatedDate = item.CreatedDate,
            OriginalDate = item.OriginalDate,
            ThumbnailPath = item.ThumbnailPath,
            IsFavorite = item.IsFavorite,
            Tags = item.Tags,
            Metadata = item.Metadata,
            Description = item.Description,
            ErrorMessage = item.ErrorMessage
        };
    }
}
