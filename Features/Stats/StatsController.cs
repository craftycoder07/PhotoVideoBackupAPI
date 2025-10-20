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
        /// Get device backup statistics
        /// </summary>
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<BackupStats>> GetDeviceStats(string deviceId)
        {
            try
            {
                var stats = await _mediaBackupService.GetDeviceStatsAsync(deviceId);
                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to get device stats", details = ex.Message });
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
