using MediatR;
using PixNestAPI.Application.Features.Users.Dtos;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Features.Users.GetUser;

public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _users;

    public GetUserHandler(IUserRepository users) => _users = users;

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct)
            ?? throw new UserNotFoundException(request.UserId);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            LastSeen = user.LastSeen,
            Settings = user.Settings,
            Stats = user.Stats
        };
    }
}
