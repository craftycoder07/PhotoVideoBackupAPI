using Microsoft.AspNetCore.Mvc;
using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Services;

namespace PhotoVideoBackupAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaBackupController : ControllerBase
    {
        private readonly IMediaBackupService _mediaBackupService;
        private readonly ILogger<MediaBackupController> _logger;

        public MediaBackupController(IMediaBackupService mediaBackupService, ILogger<MediaBackupController> logger)
        {
            _mediaBackupService = mediaBackupService;
            _logger = logger;
        }

        // Device Management Endpoints

        /// <summary>
        /// Register a new mobile device for backup
        /// </summary>
        [HttpPost("devices/register")]
        public async Task<ActionResult<Device>> RegisterDevice([FromBody] DeviceRegistrationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var device = await _mediaBackupService.RegisterDeviceAsync(request);
                _logger.LogInformation("Device registered: {DeviceName}", device.DeviceName);
                
                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
                return StatusCode(500, new { error = "Failed to register device", details = ex.Message });
            }
        }

        /// <summary>
        /// Get device information
        /// </summary>
        [HttpGet("devices/{deviceId}")]
        public async Task<ActionResult<Device>> GetDevice(string deviceId)
        {
            try
            {
                var device = await _mediaBackupService.GetDeviceAsync(deviceId);
                if (device == null)
                {
                    return NotFound(new { error = "Device not found" });
                }
                
                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to get device", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all registered devices
        /// </summary>
        [HttpGet("devices")]
        public async Task<ActionResult<List<Device>>> GetAllDevices()
        {
            try
            {
                var devices = await _mediaBackupService.GetAllDevicesAsync();
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all devices");
                return StatusCode(500, new { error = "Failed to get devices", details = ex.Message });
            }
        }

        /// <summary>
        /// Update device settings
        /// </summary>
        [HttpPut("devices/{deviceId}/settings")]
        public async Task<ActionResult<Device>> UpdateDeviceSettings(string deviceId, [FromBody] DeviceSettings settings)
        {
            try
            {
                var device = await _mediaBackupService.UpdateDeviceSettingsAsync(deviceId, settings);
                return Ok(device);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device settings for {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to update device settings", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete device and all its data
        /// </summary>
        [HttpDelete("devices/{deviceId}")]
        public async Task<ActionResult> DeleteDevice(string deviceId)
        {
            try
            {
                var deleted = await _mediaBackupService.DeleteDeviceAsync(deviceId);
                if (deleted)
                {
                    return Ok(new { message = "Device deleted successfully" });
                }
                else
                {
                    return NotFound(new { error = "Device not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to delete device", details = ex.Message });
            }
        }

        // Backup Session Endpoints

        /// <summary>
        /// Start a new backup session
        /// </summary>
        [HttpPost("devices/{deviceId}/sessions")]
        public async Task<ActionResult<BackupSession>> StartBackupSession(string deviceId, [FromBody] BackupSessionInfo sessionInfo)
        {
            try
            {
                var session = await _mediaBackupService.StartBackupSessionAsync(deviceId, sessionInfo);
                _logger.LogInformation("Backup session started: {SessionId} for device {DeviceId}", session.Id, deviceId);
                
                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting backup session for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to start backup session", details = ex.Message });
            }
        }

        /// <summary>
        /// Get backup session details
        /// </summary>
        [HttpGet("sessions/{sessionId}")]
        public async Task<ActionResult<BackupSession>> GetBackupSession(string sessionId)
        {
            try
            {
                var session = await _mediaBackupService.GetBackupSessionAsync(sessionId);
                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backup session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to get backup session", details = ex.Message });
            }
        }

        /// <summary>
        /// Update backup session progress
        /// </summary>
        [HttpPut("sessions/{sessionId}")]
        public async Task<ActionResult<BackupSession>> UpdateBackupSession(string sessionId, [FromBody] BackupSessionUpdateRequest request)
        {
            try
            {
                var session = await _mediaBackupService.UpdateBackupSessionAsync(sessionId, request);
                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating backup session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to update backup session", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all backup sessions for a device
        /// </summary>
        [HttpGet("devices/{deviceId}/sessions")]
        public async Task<ActionResult<List<BackupSession>>> GetDeviceBackupSessions(string deviceId)
        {
            try
            {
                var sessions = await _mediaBackupService.GetDeviceBackupSessionsAsync(deviceId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backup sessions for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to get backup sessions", details = ex.Message });
            }
        }

        // Media Upload Endpoints

        /// <summary>
        /// Upload media file to backup session
        /// </summary>
        [HttpPost("sessions/{sessionId}/upload")]
        public async Task<ActionResult<MediaItem>> UploadMedia(string sessionId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                var mediaItem = await _mediaBackupService.UploadMediaAsync(sessionId, file);
                _logger.LogInformation("Media uploaded: {FileName} to session {SessionId}", file.FileName, sessionId);
                
                return Ok(mediaItem);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading media to session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to upload media", details = ex.Message });
            }
        }

        /// <summary>
        /// Get media item details
        /// </summary>
        [HttpGet("media/{mediaId}")]
        public async Task<ActionResult<MediaItem>> GetMediaItem(string mediaId)
        {
            try
            {
                var mediaItem = await _mediaBackupService.GetMediaItemAsync(mediaId);
                return Ok(mediaItem);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media item {MediaId}", mediaId);
                return StatusCode(500, new { error = "Failed to get media item", details = ex.Message });
            }
        }

        /// <summary>
        /// Get device media with pagination
        /// </summary>
        [HttpGet("devices/{deviceId}/media")]
        public async Task<ActionResult<List<MediaItem>>> GetDeviceMedia(string deviceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var mediaItems = await _mediaBackupService.GetDeviceMediaAsync(deviceId, page, pageSize);
                return Ok(mediaItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to get device media", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete media item
        /// </summary>
        [HttpDelete("media/{mediaId}")]
        public async Task<ActionResult> DeleteMediaItem(string mediaId)
        {
            try
            {
                var deleted = await _mediaBackupService.DeleteMediaItemAsync(mediaId);
                if (deleted)
                {
                    return Ok(new { message = "Media item deleted successfully" });
                }
                else
                {
                    return NotFound(new { error = "Media item not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting media item {MediaId}", mediaId);
                return StatusCode(500, new { error = "Failed to delete media item", details = ex.Message });
            }
        }

        // Thumbnail Endpoints

        /// <summary>
        /// Get media thumbnail
        /// </summary>
        [HttpGet("media/{mediaId}/thumbnail")]
        public async Task<IActionResult> GetThumbnail(string mediaId)
        {
            try
            {
                var thumbnailData = await _mediaBackupService.GetThumbnailAsync(mediaId);
                var mediaItem = await _mediaBackupService.GetMediaItemAsync(mediaId);
                
                return File(thumbnailData, GetContentType(mediaItem.FileExtension));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting thumbnail for media {MediaId}", mediaId);
                return StatusCode(500, new { error = "Failed to get thumbnail", details = ex.Message });
            }
        }

        // Statistics Endpoints

        /// <summary>
        /// Get device backup statistics
        /// </summary>
        [HttpGet("devices/{deviceId}/stats")]
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
        [HttpGet("stats")]
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

        // Search Endpoints

        /// <summary>
        /// Search media items
        /// </summary>
        [HttpGet("devices/{deviceId}/search")]
        public async Task<ActionResult<List<MediaItem>>> SearchMedia(string deviceId, [FromQuery] string? query = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var mediaItems = await _mediaBackupService.SearchMediaAsync(deviceId, query ?? "", fromDate, toDate);
                return Ok(mediaItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching media for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to search media", details = ex.Message });
            }
        }

        /// <summary>
        /// Get media by date range
        /// </summary>
        [HttpGet("devices/{deviceId}/media/date-range")]
        public async Task<ActionResult<List<MediaItem>>> GetMediaByDateRange(string deviceId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var mediaItems = await _mediaBackupService.GetMediaByDateRangeAsync(deviceId, fromDate, toDate);
                return Ok(mediaItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media by date range for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to get media by date range", details = ex.Message });
            }
        }

        // Helper methods
        private string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".heic" or ".heif" => "image/heic",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                _ => "application/octet-stream"
            };
        }
    }
} 