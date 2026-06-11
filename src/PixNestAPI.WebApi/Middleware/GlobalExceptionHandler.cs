using Microsoft.AspNetCore.Diagnostics;
using PixNestAPI.Application.Common.Exceptions;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.WebApi.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (statusCode, title) = ex switch
        {
            UserNotFoundException or SessionNotFoundException or MediaItemNotFoundException => (404, "Not Found"),
            ValidationException => (400, "Validation Failed"),
            UnauthorizedAccessException => (401, "Unauthorized"),
            InvalidOperationException => (400, "Bad Request"),
            NotImplementedException => (501, "Not Implemented"),
            _ => (500, "Internal Server Error")
        };

        _logger.LogError(ex, "Unhandled exception {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";

        object response = ex is ValidationException ve
            ? new { title, errors = ve.Errors }
            : new { title, detail = ex.Message };

        await ctx.Response.WriteAsJsonAsync(response, ct);
        return true;
    }
}
