using Microsoft.AspNetCore.Mvc;
using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Services;

namespace PhotoVideoBackupAPI.Features.Media
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaBackupService _mediaBackupService;
        private readonly ILogger<MediaController> _logger;

        public MediaController(IMediaBackupService mediaBackupService, ILogger<MediaController> logger)
        {
            _mediaBackupService = mediaBackupService;
            _logger = logger;
        }

        /// <summary>
        /// Upload media file to backup session
        /// </summary>
        [HttpPost("upload/{sessionId}")]
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
        [HttpGet("{mediaId}")]
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
        [HttpGet("device/{deviceId}")]
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
        [HttpDelete("{mediaId}")]
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

        /// <summary>
        /// Get media thumbnail
        /// </summary>
        [HttpGet("{mediaId}/thumbnail")]
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

        /// <summary>
        /// Search media items
        /// </summary>
        [HttpGet("device/{deviceId}/search")]
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
        [HttpGet("device/{deviceId}/date-range")]
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
