using MediatR;
using PixNestAPI.Application.Features.Media.Dtos;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.Exceptions;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Media.Upload;

public class UploadMediaHandler : IRequestHandler<UploadMediaCommand, MediaItemDto>
{
    private static readonly string[] PhotoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".heic", ".heif" };

    private readonly ISessionRepository _sessions;
    private readonly IUserRepository _users;
    private readonly IMediaRepository _media;
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;

    public UploadMediaHandler(
        ISessionRepository sessions,
        IUserRepository users,
        IMediaRepository media,
        IUnitOfWork uow,
        IFileStorageService storage)
    {
        _sessions = sessions;
        _users = users;
        _media = media;
        _uow = uow;
        _storage = storage;
    }

    public async Task<MediaItemDto> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(request.SessionId, ct)
            ?? throw new SessionNotFoundException(request.SessionId);

        var user = await _users.GetByIdAsync(session.UserId, ct)
            ?? throw new UserNotFoundException(session.UserId);

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var mediaType = PhotoExtensions.Contains(extension) ? MediaType.Photo : MediaType.Video;

        var saved = await _storage.SaveAsync(request.FileStream, user.Username, extension, ct);

        var item = new MediaItem
        {
            SessionId = request.SessionId,
            FileName = request.FileName,
            ServerPath = saved.ServerPath,
            FileExtension = extension,
            FileSize = request.FileSize,
            Type = mediaType,
            Metadata = request.Metadata ?? new MediaMetadata { FileHash = saved.FileHash },
            Status = BackupStatus.Completed
        };
        item.Metadata.FileHash = saved.FileHash;

        _media.Add(item);

        session.TotalItems++;
        session.SuccessfulBackups++;
        session.TotalSize += request.FileSize;

        user.Stats.TotalSize += request.FileSize;
        user.Stats.LastBackupDate = DateTime.UtcNow;
        user.Stats.SuccessfulBackups++;
        if (mediaType == MediaType.Photo) user.Stats.TotalPhotos++;
        else user.Stats.TotalVideos++;

        await _uow.SaveChangesAsync(ct);

        return ToDto(item);
    }

    private static MediaItemDto ToDto(MediaItem m) => new()
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
    };
}
