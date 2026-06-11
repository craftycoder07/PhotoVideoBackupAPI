using Microsoft.EntityFrameworkCore;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;

namespace PixNestAPI.Infrastructure.Persistence.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly AppDbContext _context;

    public MediaRepository(AppDbContext context) => _context = context;

    public async Task<MediaItem?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _context.MediaItems.FindAsync(new object[] { id }, ct);

    public async Task<List<MediaItem>> GetUserMediaAsync(string userId, int page, int pageSize, CancellationToken ct = default)
        => await _context.MediaItems
            .Include(m => m.Session)
            .Where(m => m.Session.UserId == userId)
            .OrderByDescending(m => m.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<List<MediaItem>> SearchAsync(string userId, string query, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var q = _context.MediaItems.Include(m => m.Session).Where(m => m.Session.UserId == userId);

        if (!string.IsNullOrEmpty(query))
            q = q.Where(m => m.FileName.Contains(query) || (m.Description != null && m.Description.Contains(query)));
        if (fromDate.HasValue) q = q.Where(m => m.CreatedDate >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(m => m.CreatedDate <= toDate.Value);

        return await q.OrderByDescending(m => m.CreatedDate).ToListAsync(ct);
    }

    public async Task<List<MediaItem>> GetByDateRangeAsync(string userId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.MediaItems
            .Include(m => m.Session)
            .Where(m => m.Session.UserId == userId && m.CreatedDate >= from && m.CreatedDate <= to)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.MediaItems.CountAsync(ct);

    public async Task<int> CountByTypeAsync(MediaType type, CancellationToken ct = default)
        => await _context.MediaItems.CountAsync(m => m.Type == type, ct);

    public async Task<long> SumFileSizeAsync(CancellationToken ct = default)
        => await _context.MediaItems.AnyAsync(ct)
            ? await _context.MediaItems.SumAsync(m => m.FileSize, ct)
            : 0L;

    public void Add(MediaItem item) => _context.MediaItems.Add(item);

    public void Remove(MediaItem item) => _context.MediaItems.Remove(item);
}
