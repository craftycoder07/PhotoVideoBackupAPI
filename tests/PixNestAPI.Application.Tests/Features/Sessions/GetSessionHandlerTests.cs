using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Sessions.Get;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Sessions;

public class GetSessionHandlerTests
{
    private readonly Mock<ISessionRepository> _sessions = new();

    private GetSessionHandler CreateHandler() => new(_sessions.Object);

    [Fact]
    public async Task Handle_ExistingSession_ReturnsMappedDto()
    {
        var session = TestData.Session(status: SessionStatus.Completed);
        session.TotalItems = 12;
        session.SuccessfulBackups = 11;
        // Handler loads via GetByIdWithItemsAsync (eager-loads Items), not GetByIdAsync.
        _sessions.Setup(r => r.GetByIdWithItemsAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var result = await CreateHandler().Handle(new GetSessionQuery(session.Id), CancellationToken.None);

        result.Id.Should().Be(session.Id);
        result.Status.Should().Be(SessionStatus.Completed);
        result.TotalItems.Should().Be(12);
        result.SuccessfulBackups.Should().Be(11);
    }

    [Fact]
    public async Task Handle_MissingSession_ThrowsSessionNotFound()
    {
        _sessions.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BackupSession?)null);

        var act = () => CreateHandler().Handle(new GetSessionQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<SessionNotFoundException>();
    }
}
