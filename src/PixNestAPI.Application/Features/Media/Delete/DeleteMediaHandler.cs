using MediatR;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;

namespace PixNestAPI.Application.Features.Media.Delete;

public class DeleteMediaHandler : IRequestHandler<DeleteMediaCommand, bool>
{
    private readonly IMediaRepository _media;
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;

    public DeleteMediaHandler(IMediaRepository media, IUnitOfWork uow, IFileStorageService storage)
    {
        _media = media;
        _uow = uow;
        _storage = storage;
    }

    public async Task<bool> Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var item = await _media.GetByIdAsync(request.MediaId, ct);
        if (item == null) return false;

        _media.Remove(item);
        await _uow.SaveChangesAsync(ct);

        await _storage.DeleteAsync(item.ServerPath);

        if (!string.IsNullOrEmpty(item.ThumbnailPath))
            await _storage.DeleteAsync(item.ThumbnailPath);

        return true;
    }
}
