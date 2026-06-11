using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixNestAPI.Application.Features.Media.Delete;
using PixNestAPI.Application.Features.Media.Dtos;
using PixNestAPI.Application.Features.Media.GetItem;
using PixNestAPI.Application.Features.Media.GetThumbnail;
using PixNestAPI.Application.Features.Media.GetUserMedia;
using PixNestAPI.Application.Features.Media.Search;
using PixNestAPI.Application.Features.Media.Upload;

namespace PixNestAPI.WebApi.Features.Media;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator) => _mediator = mediator;

    private string GetCurrentUserId()
        => User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpPost("upload/{sessionId}")]
    public async Task<ActionResult<MediaItemDto>> UploadMedia(string sessionId, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new UploadMediaCommand(sessionId, stream, file.FileName, file.ContentType, file.Length, null), ct);
        return Ok(result);
    }

    [HttpGet("{mediaId}")]
    public async Task<ActionResult<MediaItemDto>> GetMediaItem(string mediaId, CancellationToken ct)
    {
        var item = await _mediator.Send(new GetMediaItemQuery(mediaId), ct);
        return Ok(item);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<MediaItemDto>>> GetUserMedia([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var items = await _mediator.Send(new GetUserMediaQuery(userId, page, pageSize), ct);
        return Ok(items);
    }

    [HttpDelete("{mediaId}")]
    public async Task<ActionResult> DeleteMediaItem(string mediaId, CancellationToken ct)
    {
        var deleted = await _mediator.Send(new DeleteMediaCommand(mediaId), ct);
        return deleted ? NoContent() : NotFound(new { error = "Media item not found" });
    }

    [HttpGet("{mediaId}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string mediaId, CancellationToken ct)
    {
        var item = await _mediator.Send(new GetMediaItemQuery(mediaId), ct);
        var bytes = await _mediator.Send(new GetThumbnailQuery(mediaId), ct);
        return File(bytes, GetContentType(item.FileExtension));
    }

    [HttpGet("search")]
    [Authorize]
    public async Task<ActionResult<List<MediaItemDto>>> SearchUserMedia(
        [FromQuery] string? query = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var items = await _mediator.Send(new SearchMediaQuery(userId, query ?? "", fromDate, toDate), ct);
        return Ok(items);
    }

    [HttpGet("date-range")]
    [Authorize]
    public async Task<ActionResult<List<MediaItemDto>>> GetMediaByDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        // SearchAsync with empty query falls through to pure date filtering
        var items = await _mediator.Send(new SearchMediaQuery(userId, "", fromDate, toDate), ct);
        return Ok(items);
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
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
