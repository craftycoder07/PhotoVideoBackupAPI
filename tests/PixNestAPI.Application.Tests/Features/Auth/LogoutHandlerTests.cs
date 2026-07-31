using FluentAssertions;
using PixNestAPI.Application.Features.Auth.Logout;

namespace PixNestAPI.Application.Tests.Features.Auth;

public class LogoutHandlerTests
{
    // Token invalidation isn't implemented yet (no token store), so logout is a
    // best-effort no-op that always succeeds. This test pins that contract so a
    // future implementation that changes it is a deliberate, visible change.
    [Fact]
    public async Task Handle_AlwaysReturnsTrue()
    {
        var result = await new LogoutHandler().Handle(new LogoutCommand("any-refresh-token"), CancellationToken.None);

        result.Should().BeTrue();
    }
}
