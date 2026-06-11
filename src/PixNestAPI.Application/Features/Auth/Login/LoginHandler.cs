using MediatR;
using PixNestAPI.Application.Features.Auth.Dtos;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;

namespace PixNestAPI.Application.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginHandler(IUserRepository users, IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(request.Username, ct);

        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        user.LastLoginAt = DateTime.UtcNow;
        user.LastSeen = DateTime.UtcNow;
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
