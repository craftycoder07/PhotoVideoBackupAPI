using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhotoVideoBackupAPI.Models;
using PhotoVideoBackupAPI.Services;
using System.Security.Claims;

namespace PhotoVideoBackupAPI.Features.Sessions
{
    [ApiController]
    [Route("api/session")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly IMediaBackupService _mediaBackupService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(IMediaBackupService mediaBackupService, ILogger<SessionController> logger)
        {
            _mediaBackupService = mediaBackupService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");
        }

        /// <summary>
        /// Start a new backup session
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<BackupSession>> StartBackupSession([FromBody] StartSessionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _mediaBackupService.StartBackupSessionAsync(userId, request.SessionInfo);
                _logger.LogInformation("Backup session started: {SessionId} by user {UserId}", session.Id, userId);
                
                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting backup session");
                return StatusCode(500, new { error = "Failed to start backup session", details = ex.Message });
            }
        }

        /// <summary>
        /// Get backup session details
        /// </summary>
        [HttpGet("{sessionId}")]
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
        [HttpPut("{sessionId}")]
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
        /// Get all backup sessions for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<BackupSession>>> GetUserBackupSessions()
        {
            try
            {
                var userId = GetCurrentUserId();
                var sessions = await _mediaBackupService.GetUserBackupSessionsAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backup sessions for user");
                return StatusCode(500, new { error = "Failed to get backup sessions", details = ex.Message });
            }
        }

    }

    public class StartSessionRequest
    {
        public BackupSessionInfo SessionInfo { get; set; } = new();
    }
}
