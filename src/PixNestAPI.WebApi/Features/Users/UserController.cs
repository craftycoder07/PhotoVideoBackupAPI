using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixNestAPI.Application.Features.Users.Dtos;
using PixNestAPI.Application.Features.Users.GetUser;
using PixNestAPI.Application.Features.Users.UpdateSettings;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.WebApi.Features.Users;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) => _mediator = mediator;

    private string GetCurrentUserId()
        => User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpGet]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var user = await _mediator.Send(new GetUserQuery(userId), ct);
        return Ok(user);
    }

    [HttpPut("settings")]
    public async Task<ActionResult<UserDto>> UpdateUserSettings([FromBody] DeviceSettings settings, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var user = await _mediator.Send(new UpdateSettingsCommand(userId, settings), ct);
        return Ok(user);
    }
}
