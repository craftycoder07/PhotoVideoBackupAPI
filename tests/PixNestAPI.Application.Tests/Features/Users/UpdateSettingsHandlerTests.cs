using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Users.UpdateSettings;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Exceptions;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Tests.Features.Users;

public class UpdateSettingsHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateSettingsHandler CreateHandler() => new(_users.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingUser_ReplacesSettingsUpdatesLastSeenAndSaves()
    {
        var user = TestData.User();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var newSettings = new DeviceSettings { AutoBackupEnabled = false, BackupOnlyOnWifi = false, ImageQuality = 60 };

        var before = DateTime.UtcNow;
        var result = await CreateHandler().Handle(new UpdateSettingsCommand(user.Id, newSettings), CancellationToken.None);

        user.Settings.Should().BeSameAs(newSettings);
        user.LastSeen.Should().BeOnOrAfter(before);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Settings.AutoBackupEnabled.Should().BeFalse();
        result.Settings.ImageQuality.Should().Be(60);
    }

    [Fact]
    public async Task Handle_MissingUser_ThrowsUserNotFoundAndDoesNotSave()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler()
            .Handle(new UpdateSettingsCommand("missing", new DeviceSettings()), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
