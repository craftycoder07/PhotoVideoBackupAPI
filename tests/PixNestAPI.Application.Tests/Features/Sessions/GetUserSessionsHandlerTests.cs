using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Sessions.GetUserSessions;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Tests.Features.Sessions;

public class GetUserSessionsHandlerTests
{
    private readonly Mock<ISessionRepository> _sessions = new();

    private GetUserSessionsHandler CreateHandler() => new(_sessions.Object);

    [Fact]
    public async Task Handle_MapsRepositorySessionsToDtosPreservingOrder()
    {
        var first = TestData.Session(userId: "user-1");
        var second = TestData.Session(userId: "user-1");
        _sessions.Setup(r => r.GetUserSessionsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupSession> { first, second });

        var result = await CreateHandler().Handle(new GetUserSessionsQuery("user-1"), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(first.Id);
        result[1].Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task Handle_NoSessions_ReturnsEmptyList()
    {
        _sessions.Setup(r => r.GetUserSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupSession>());

        var result = await CreateHandler().Handle(new GetUserSessionsQuery("user-1"), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
