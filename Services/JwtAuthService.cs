using Microsoft.IdentityModel.Tokens;
using PhotoVideoBackupAPI.Data;
using PhotoVideoBackupAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PhotoVideoBackupAPI.Services
{
    public class JwtAuthService : IAuthService
    {
        private readonly MediaBackupDbContext _context;
        private readonly ILogger<JwtAuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly int _jwtExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;
        private readonly string _baseStoragePath;

        public JwtAuthService(MediaBackupDbContext context, ILogger<JwtAuthService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _jwtSecret = _configuration["Jwt:Secret"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
            _jwtIssuer = _configuration["Jwt:Issuer"] ?? "PhotoVideoBackupAPI";
            _jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
            _refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
            _baseStoragePath = _configuration["StorageSettings:BasePath"] ?? "/tmp/MediaBackup";
            
            // Ensure base directory exists
            Directory.CreateDirectory(_baseStoragePath);
        }

        public async Task<AuthResponse> RegisterAsync(UserRegistrationRequest request)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Hash password
            var passwordHash = HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create user storage directory
            var userPath = Path.Combine(_baseStoragePath, user.Id);
            Directory.CreateDirectory(userPath);

            _logger.LogInformation("User registered: {Username} with storage path: {UserPath}", user.Username, userPath);

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Store refresh token (in a real app, you'd store this in a separate table)
            // For simplicity, we'll just return it

            return new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
            };
        }

        public async Task<AuthResponse> LoginAsync(UserLoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is deactivated");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in: {Username}", user.Username);

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            return new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes)
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // In a real implementation, you'd validate the refresh token against a stored token
            // For simplicity, we'll just generate a new token
            // You should implement proper refresh token validation and storage

            var refreshToken = request.RefreshToken;
            
            // For now, we'll assume the refresh token is valid and extract user info
            // In a real app, you'd have a separate refresh token table
            throw new NotImplementedException("Refresh token validation not fully implemented");
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            // In a real implementation, you'd invalidate the refresh token
            // For now, we'll just return true
            _logger.LogInformation("User logged out");
            return await Task.FromResult(true);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        public async Task<string> GetUserIdFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("Invalid token");
                }

                return await Task.FromResult(userId);
            }
            catch
            {
                throw new UnauthorizedAccessException("Invalid token");
            }
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

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
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                Issuer = _jwtIssuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string HashPassword(string password)
        {
            // Use PBKDF2 for password hashing
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            var hashBytes = new byte[48]; // 16 (salt) + 32 (hash)
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            return Convert.ToBase64String(hashBytes);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(storedHash);
                var salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(32);

                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

