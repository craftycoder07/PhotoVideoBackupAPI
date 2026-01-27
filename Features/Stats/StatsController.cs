using Microsoft.AspNetCore.Mvc;
using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Services;

namespace PhotoVideoBackupAPI.Features.Stats
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : ControllerBase
    {
        private readonly IMediaBackupService _mediaBackupService;
        private readonly ILogger<StatsController> _logger;

        public StatsController(IMediaBackupService mediaBackupService, ILogger<StatsController> logger)
        {
            _mediaBackupService = mediaBackupService;
            _logger = logger;
        }

        /// <summary>
        /// Get user backup statistics
        /// </summary>
        [HttpGet("user/{userId}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<BackupStats>> GetUserStats(string userId)
        {
            try
            {
                // Verify the user is accessing their own stats
                var currentUserId = User.FindFirst("userId")?.Value;
                if (currentUserId != userId)
                {
                    return Unauthorized(new { error = "Unauthorized to access this user's stats" });
                }

                var stats = await _mediaBackupService.GetUserStatsAsync(userId);
                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to get user stats", details = ex.Message });
            }
        }

        /// <summary>
        /// Get system-wide statistics
        /// </summary>
        [HttpGet("system")]
        public async Task<ActionResult<SystemStats>> GetSystemStats()
        {
            try
            {
                var stats = await _mediaBackupService.GetSystemStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system stats");
                return StatusCode(500, new { error = "Failed to get system stats", details = ex.Message });
            }
        }
    }
}
