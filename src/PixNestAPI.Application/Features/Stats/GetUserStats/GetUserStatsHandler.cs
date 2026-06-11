using MediatR;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Exceptions;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Stats.GetUserStats;

public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, BackupStats>
{
    private readonly IUserRepository _users;

    public GetUserStatsHandler(IUserRepository users) => _users = users;

    public async Task<BackupStats> Handle(GetUserStatsQuery request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct)
            ?? throw new UserNotFoundException(request.UserId);

        return user.Stats;
    }
}
