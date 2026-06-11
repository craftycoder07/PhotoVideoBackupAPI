using MediatR;
using PixNestAPI.Application.Features.Stats.Dtos;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Enums;

namespace PixNestAPI.Application.Features.Stats.GetSystemStats;

public class GetSystemStatsHandler : IRequestHandler<GetSystemStatsQuery, SystemStatsDto>
{
    private readonly IUserRepository _users;
    private readonly IMediaRepository _media;
    private readonly IFileStorageService _storage;

    public GetSystemStatsHandler(IUserRepository users, IMediaRepository media, IFileStorageService storage)
    {
        _users = users;
        _media = media;
        _storage = storage;
    }

    public async Task<SystemStatsDto> Handle(GetSystemStatsQuery request, CancellationToken ct)
    {
        var totalUsers = await _users.CountAsync(ct);
        var activeUsers = await _users.CountActiveAsync(ct);
        var lastActivity = await _users.GetLastSeenAsync(ct);
        var totalMedia = await _media.CountAsync(ct);
        var totalPhotos = await _media.CountByTypeAsync(MediaType.Photo, ct);
        var totalVideos = await _media.CountByTypeAsync(MediaType.Video, ct);
        var totalSize = await _media.SumFileSizeAsync(ct);

        return new SystemStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            LastBackupActivity = lastActivity,
            TotalMediaItems = totalMedia,
            TotalPhotos = totalPhotos,
            TotalVideos = totalVideos,
            TotalStorageUsed = totalSize,
            AvailableStorage = _storage.GetAvailableStorage()
        };
    }
}
