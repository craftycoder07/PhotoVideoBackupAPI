using Microsoft.EntityFrameworkCore;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _context.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Username == username, ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Users.CountAsync(ct);

    public async Task<int> CountActiveAsync(CancellationToken ct = default)
        => await _context.Users.CountAsync(u => u.IsActive, ct);

    public async Task<DateTime> GetLastSeenAsync(CancellationToken ct = default)
        => await _context.Users.AnyAsync(ct)
            ? await _context.Users.MaxAsync(u => u.LastSeen, ct)
            : DateTime.MinValue;

    public void Add(User user) => _context.Users.Add(user);
}
