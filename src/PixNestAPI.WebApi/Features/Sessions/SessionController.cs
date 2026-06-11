using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixNestAPI.Application.Features.Sessions.Dtos;
using PixNestAPI.Application.Features.Sessions.Get;
using PixNestAPI.Application.Features.Sessions.GetUserSessions;
using PixNestAPI.Application.Features.Sessions.Start;
using PixNestAPI.Application.Features.Sessions.Update;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.WebApi.Features.Sessions;

[ApiController]
[Route("api/session")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionController(IMediator mediator) => _mediator = mediator;

    private string GetCurrentUserId()
        => User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpPost("start")]
    public async Task<ActionResult<BackupSessionDto>> StartBackupSession([FromBody] StartSessionRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var session = await _mediator.Send(new StartSessionCommand(userId, request.SessionInfo), ct);
        return Ok(session);
    }

    [HttpGet("{sessionId}")]
    public async Task<ActionResult<BackupSessionDto>> GetBackupSession(string sessionId, CancellationToken ct)
    {
        var session = await _mediator.Send(new GetSessionQuery(sessionId), ct);
        return Ok(session);
    }

    [HttpPut("{sessionId}")]
    public async Task<ActionResult<BackupSessionDto>> UpdateBackupSession(string sessionId, [FromBody] UpdateSessionRequest request, CancellationToken ct)
    {
        var session = await _mediator.Send(new UpdateSessionCommand(
            sessionId,
            request.ProcessedItems,
            request.SuccessfulBackups,
            request.FailedBackups,
            request.SkippedItems,
            request.TotalSize,
            request.Status,
            request.ErrorMessage), ct);
        return Ok(session);
    }

    [HttpGet]
    public async Task<ActionResult<List<BackupSessionDto>>> GetUserBackupSessions(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var sessions = await _mediator.Send(new GetUserSessionsQuery(userId), ct);
        return Ok(sessions);
    }
}

public record StartSessionRequest(BackupSessionInfo SessionInfo);
public record UpdateSessionRequest(
    int? ProcessedItems,
    int? SuccessfulBackups,
    int? FailedBackups,
    int? SkippedItems,
    long? TotalSize,
    SessionStatus? Status,
    string? ErrorMessage);
