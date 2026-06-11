using MediatR;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Features.Media.GetThumbnail;

public class GetThumbnailHandler : IRequestHandler<GetThumbnailQuery, byte[]>
{
    private readonly IMediaRepository _media;
    private readonly IUnitOfWork _uow;
    private readonly IThumbnailService _thumbnails;

    public GetThumbnailHandler(IMediaRepository media, IUnitOfWork uow, IThumbnailService thumbnails)
    {
        _media = media;
        _uow = uow;
        _thumbnails = thumbnails;
    }

    public async Task<byte[]> Handle(GetThumbnailQuery request, CancellationToken ct)
    {
        var item = await _media.GetByIdAsync(request.MediaId, ct)
            ?? throw new MediaItemNotFoundException(request.MediaId);

        // Generate on-the-fly if not yet created
        if (string.IsNullOrEmpty(item.ThumbnailPath))
        {
            var path = await _thumbnails.GenerateAsync(item.Id, item.ServerPath, item.Type);
            item.ThumbnailPath = path;
            await _uow.SaveChangesAsync(ct);
        }

        return await _thumbnails.GetBytesAsync(item.ThumbnailPath);
    }
}
