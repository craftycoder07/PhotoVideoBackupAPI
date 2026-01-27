using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Services;
using System.Security.Claims;

namespace PhotoVideoBackupAPI.Features.Users
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IMediaBackupService _mediaBackupService;
        private readonly ILogger<UserController> _logger;

        public UserController(IMediaBackupService mediaBackupService, ILogger<UserController> logger)
        {
            _mediaBackupService = mediaBackupService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _mediaBackupService.GetUserAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }
                
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { error = "Failed to get user", details = ex.Message });
            }
        }

        /// <summary>
        /// Update user settings
        /// </summary>
        [HttpPut("settings")]
        public async Task<ActionResult<User>> UpdateUserSettings([FromBody] DeviceSettings settings)
        {
            try
            {
                var userId = GetCurrentUserId();
                var updatedUser = await _mediaBackupService.UpdateUserSettingsAsync(userId, settings);
                return Ok(updatedUser);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings");
                return StatusCode(500, new { error = "Failed to update user settings", details = ex.Message });
            }
        }
    }
}


