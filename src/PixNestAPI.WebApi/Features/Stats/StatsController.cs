using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixNestAPI.Application.Features.Stats.Dtos;
using PixNestAPI.Application.Features.Stats.GetSystemStats;
using PixNestAPI.Application.Features.Stats.GetUserStats;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.WebApi.Features.Stats;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<ActionResult<BackupStats>> GetUserStats(string userId, CancellationToken ct)
    {
        var currentUserId = User.FindFirst("userId")?.Value;
        if (currentUserId != userId)
            return Unauthorized(new { error = "Unauthorized to access this user's stats" });

        var stats = await _mediator.Send(new GetUserStatsQuery(userId), ct);
        return Ok(stats);
    }

    [HttpGet("system")]
    public async Task<ActionResult<SystemStatsDto>> GetSystemStats(CancellationToken ct)
    {
        var stats = await _mediator.Send(new GetSystemStatsQuery(), ct);
        return Ok(stats);
    }
}
