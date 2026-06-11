using MediatR;
using PixNestAPI.Application.Features.Auth.Dtos;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public RegisterHandler(IUserRepository users, IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (await _users.UsernameExistsAsync(request.Username, ct))
            throw new InvalidOperationException("Username already exists.");

        if (await _users.EmailExistsAsync(request.Email, ct))
            throw new InvalidOperationException("Email already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            RegisteredDate = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        _users.Add(user);
        await _uow.SaveChangesAsync(ct);

        var accessToken = _tokens.GenerateAccessToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            AccessToken = accessToken.Value,
            RefreshToken = _tokens.GenerateRefreshToken(),
            ExpiresAt = accessToken.ExpiresAt
        };
    }
}
