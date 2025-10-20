using Microsoft.AspNetCore.Mvc;
using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Services;

namespace PhotoVideoBackupAPI.Features.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] UserRegistrationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _authService.RegisterAsync(request);
                _logger.LogInformation("User registered: {Username}", response.Username);
                
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { error = "Registration failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] UserLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _authService.LoginAsync(request);
                _logger.LogInformation("User logged in: {Username}", response.Username);
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return StatusCode(500, new { error = "Authentication failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request);
                _logger.LogInformation("Token refreshed for user");
                
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { error = "Token refresh failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Logout and invalidate token
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var success = await _authService.LogoutAsync(request.RefreshToken);
                if (success)
                {
                    _logger.LogInformation("User logged out successfully");
                    return Ok(new { message = "Logged out successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Logout failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { error = "Logout failed", details = ex.Message });
            }
        }
    }
}
