using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Services;
using System.Security.Claims;

namespace PhotoVideoBackupAPI.Features.Devices
{
    [ApiController]
    [Route("api/device")]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        private readonly IMediaBackupService _mediaBackupService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(IMediaBackupService mediaBackupService, ILogger<DeviceController> logger)
        {
            _mediaBackupService = mediaBackupService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");
        }

        /// <summary>
        /// Register a new mobile device for backup
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<Device>> RegisterDevice([FromBody] DeviceRegistrationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                request.UserId = userId; // Set the user ID from the authenticated user
                
                var device = await _mediaBackupService.RegisterDeviceAsync(request);
                _logger.LogInformation("Device registered: {DeviceName} for user {UserId}", device.DeviceName, userId);
                
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
        [HttpGet("{deviceId}")]
        public async Task<ActionResult<Device>> GetDevice(string deviceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var device = await _mediaBackupService.GetDeviceAsync(deviceId);
                if (device == null || device.UserId != userId)
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
        /// Get all registered devices for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Device>>> GetAllDevices()
        {
            try
            {
                var userId = GetCurrentUserId();
                var devices = await _mediaBackupService.GetUserDevicesAsync(userId);
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
        [HttpPut("{deviceId}/settings")]
        public async Task<ActionResult<Device>> UpdateDeviceSettings(string deviceId, [FromBody] DeviceSettings settings)
        {
            try
            {
                var userId = GetCurrentUserId();
                var device = await _mediaBackupService.GetDeviceAsync(deviceId);
                if (device == null || device.UserId != userId)
                {
                    return NotFound(new { error = "Device not found" });
                }

                var updatedDevice = await _mediaBackupService.UpdateDeviceSettingsAsync(deviceId, settings);
                return Ok(updatedDevice);
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
        [HttpDelete("{deviceId}")]
        public async Task<ActionResult> DeleteDevice(string deviceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var device = await _mediaBackupService.GetDeviceAsync(deviceId);
                if (device == null || device.UserId != userId)
                {
                    return NotFound(new { error = "Device not found" });
                }

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
    }
}
