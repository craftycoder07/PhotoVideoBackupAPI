using MediatR;
using PixNestAPI.Application.Features.Users.Dtos;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Features.Users.UpdateSettings;

public class UpdateSettingsHandler : IRequestHandler<UpdateSettingsCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public UpdateSettingsHandler(IUserRepository users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<UserDto> Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct)
            ?? throw new UserNotFoundException(request.UserId);

        user.Settings = request.Settings;
        user.LastSeen = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

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
