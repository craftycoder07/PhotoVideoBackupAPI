using PhotoVideoBackupAPI.Models;

namespace PhotoVideoBackupAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(UserRegistrationRequest request);
        Task<AuthResponse> LoginAsync(UserLoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> LogoutAsync(string refreshToken);
        Task<User?> GetUserByIdAsync(string userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> ValidateTokenAsync(string token);
        Task<string> GetUserIdFromTokenAsync(string token);
    }
}

