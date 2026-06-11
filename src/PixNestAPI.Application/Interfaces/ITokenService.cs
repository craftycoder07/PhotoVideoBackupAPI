using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Interfaces;

public record AccessToken(string Value, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string? GetUserIdFromToken(string token);
}
