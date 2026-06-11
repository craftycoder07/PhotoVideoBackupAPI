using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Infrastructure.Auth;

public class JwtTokenService : ITokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly int _expirationMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
        _issuer = configuration["Jwt:Issuer"] ?? "PixNestAPI";
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");
    }

    public AccessToken GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secret));
        var expiry = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("userId", user.Id),
                new Claim("username", user.Username),
                new Claim("email", user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            }),
            Expires = expiry,
            Issuer = _issuer,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return new AccessToken(handler.WriteToken(token), expiry);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        }
        catch
        {
            return null;
        }
    }
}
