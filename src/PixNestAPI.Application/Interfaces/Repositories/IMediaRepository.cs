using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;

namespace PixNestAPI.Application.Interfaces.Repositories;

public interface IMediaRepository
{
    Task<MediaItem?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<MediaItem>> GetUserMediaAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<List<MediaItem>> SearchAsync(string userId, string query, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task<List<MediaItem>> GetByDateRangeAsync(string userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountByTypeAsync(MediaType type, CancellationToken ct = default);
    Task<long> SumFileSizeAsync(CancellationToken ct = default);
    void Add(MediaItem item);
    void Remove(MediaItem item);
}
