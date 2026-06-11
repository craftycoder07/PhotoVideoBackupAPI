using Microsoft.EntityFrameworkCore;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Infrastructure.Persistence.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _context;

    public SessionRepository(AppDbContext context) => _context = context;

    public async Task<BackupSession?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _context.BackupSessions.FindAsync(new object[] { id }, ct);

    public async Task<BackupSession?> GetByIdWithItemsAsync(string id, CancellationToken ct = default)
        => await _context.BackupSessions
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<BackupSession>> GetUserSessionsAsync(string userId, CancellationToken ct = default)
        => await _context.BackupSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(ct);

    public void Add(BackupSession session) => _context.BackupSessions.Add(session);
}
