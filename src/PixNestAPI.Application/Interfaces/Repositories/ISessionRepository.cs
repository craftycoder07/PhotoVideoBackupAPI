using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Interfaces.Repositories;

public interface ISessionRepository
{
    Task<BackupSession?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<BackupSession?> GetByIdWithItemsAsync(string id, CancellationToken ct = default);
    Task<List<BackupSession>> GetUserSessionsAsync(string userId, CancellationToken ct = default);
    void Add(BackupSession session);
}
