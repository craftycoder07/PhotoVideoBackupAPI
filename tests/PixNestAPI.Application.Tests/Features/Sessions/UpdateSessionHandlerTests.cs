using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Sessions.Update;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Sessions;

public class UpdateSessionHandlerTests
{
    private readonly Mock<ISessionRepository> _sessions = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateSessionHandler CreateHandler() => new(_sessions.Object, _uow.Object);

    private static UpdateSessionCommand Command(
        string sessionId,
        int? processed = null,
        int? successful = null,
        int? failed = null,
        int? skipped = null,
        long? totalSize = null,
        SessionStatus? status = null,
        string? error = null)
        => new(sessionId, processed, successful, failed, skipped, totalSize, status, error);

    [Fact]
    public async Task Handle_ProvidedFields_AppliedAndSaved()
    {
        var session = TestData.Session(status: SessionStatus.InProgress);
        _sessions.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var result = await CreateHandler().Handle(
            Command(session.Id, processed: 10, successful: 8, failed: 2, skipped: 1, totalSize: 4096, error: "boom"),
            CancellationToken.None);

        session.ProcessedItems.Should().Be(10);
        session.SuccessfulBackups.Should().Be(8);
        session.FailedBackups.Should().Be(2);
        session.SkippedItems.Should().Be(1);
        session.TotalSize.Should().Be(4096);
        session.ErrorMessage.Should().Be("boom");
        result.ProcessedItems.Should().Be(10);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullFields_LeavesExistingValuesUnchanged()
    {
        var session = TestData.Session();
        session.ProcessedItems = 5;
        session.TotalSize = 1000;
        _sessions.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        await CreateHandler().Handle(Command(session.Id, successful: 3), CancellationToken.None);

        session.ProcessedItems.Should().Be(5);
        session.TotalSize.Should().Be(1000);
        session.SuccessfulBackups.Should().Be(3);
    }

    [Theory]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Failed)]
    public async Task Handle_TerminalStatus_SetsEndTime(SessionStatus status)
    {
        var session = TestData.Session(status: SessionStatus.InProgress);
        _sessions.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var before = DateTime.UtcNow;
        await CreateHandler().Handle(Command(session.Id, status: status), CancellationToken.None);

        session.Status.Should().Be(status);
        session.EndTime.Should().NotBeNull();
        session.EndTime!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Handle_StillInProgress_DoesNotSetEndTime()
    {
        var session = TestData.Session(status: SessionStatus.InProgress);
        _sessions.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        await CreateHandler().Handle(Command(session.Id, processed: 1), CancellationToken.None);

        session.EndTime.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MissingSession_ThrowsSessionNotFound()
    {
        _sessions.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BackupSession?)null);

        var act = () => CreateHandler().Handle(Command("missing", processed: 1), CancellationToken.None);

        await act.Should().ThrowAsync<SessionNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
