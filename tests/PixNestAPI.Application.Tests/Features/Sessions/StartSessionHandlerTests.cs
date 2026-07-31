using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Sessions.Start;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Sessions;

public class StartSessionHandlerTests
{
    private readonly Mock<ISessionRepository> _sessions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private StartSessionHandler CreateHandler() =>
        new(_sessions.Object, _users.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingUser_CreatesInProgressSessionAndReturnsDto()
    {
        var user = TestData.User();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        BackupSession? added = null;
        _sessions.Setup(s => s.Add(It.IsAny<BackupSession>())).Callback<BackupSession>(s => added = s);

        var result = await CreateHandler()
            .Handle(new StartSessionCommand(user.Id, TestData.SessionInfo("Pixel 8")), CancellationToken.None);

        added.Should().NotBeNull();
        added!.UserId.Should().Be(user.Id);
        added.Status.Should().Be(SessionStatus.InProgress);
        added.SessionInfo.DeviceName.Should().Be("Pixel 8");
        result.Id.Should().Be(added.Id);
        result.UserId.Should().Be(user.Id);
        result.Status.Should().Be(SessionStatus.InProgress);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingUser_UpdatesLastSeen()
    {
        var user = TestData.User();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var before = DateTime.UtcNow;
        await CreateHandler().Handle(new StartSessionCommand(user.Id, TestData.SessionInfo()), CancellationToken.None);

        user.LastSeen.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Handle_MissingUser_ThrowsUserNotFound()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler()
            .Handle(new StartSessionCommand("missing", TestData.SessionInfo()), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
        _sessions.Verify(s => s.Add(It.IsAny<BackupSession>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
