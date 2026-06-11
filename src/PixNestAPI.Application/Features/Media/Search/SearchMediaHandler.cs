using MediatR;
using PixNestAPI.Application.Features.Media.Dtos;
using PixNestAPI.Application.Interfaces.Repositories;

namespace PixNestAPI.Application.Features.Media.Search;

public class SearchMediaHandler : IRequestHandler<SearchMediaQuery, List<MediaItemDto>>
{
    private readonly IMediaRepository _media;

    public SearchMediaHandler(IMediaRepository media) => _media = media;

    public async Task<List<MediaItemDto>> Handle(SearchMediaQuery request, CancellationToken ct)
    {
        var items = await _media.SearchAsync(request.UserId, request.Query, request.FromDate, request.ToDate, ct);

        return items.Select(m => new MediaItemDto
        {
            Id = m.Id,
            SessionId = m.SessionId,
            FileName = m.FileName,
            FileSize = m.FileSize,
            FileExtension = m.FileExtension,
            Type = m.Type,
            Status = m.Status,
            CreatedDate = m.CreatedDate,
            OriginalDate = m.OriginalDate,
            ThumbnailPath = m.ThumbnailPath,
            IsFavorite = m.IsFavorite,
            Tags = m.Tags,
            Metadata = m.Metadata,
            Description = m.Description,
            ErrorMessage = m.ErrorMessage
        }).ToList();
    }
}
