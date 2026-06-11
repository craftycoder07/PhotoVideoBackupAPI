using MediatR;

namespace PixNestAPI.Application.Features.Auth.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand, bool>
{
    // Refresh token storage is not yet implemented — true logout (token invalidation)
    // will be wired in once a token store (e.g. Redis or DB table) is added.
    public Task<bool> Handle(LogoutCommand request, CancellationToken ct)
        => Task.FromResult(true);
}
