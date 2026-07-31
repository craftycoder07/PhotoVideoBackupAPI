using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Auth.Login;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Tests.Features.Auth;

public class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();

    private LoginHandler CreateHandler() =>
        new(_users.Object, _uow.Object, _hasher.Object, _tokens.Object);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponseWithTokens()
    {
        var user = TestData.User(username: "alice", passwordHash: "stored-hash");
        var expiresAt = DateTime.UtcNow.AddMinutes(60);
        _users.Setup(r => r.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("pw", "stored-hash")).Returns(true);
        _tokens.Setup(t => t.GenerateAccessToken(user)).Returns(new AccessToken("access-token", expiresAt));
        _tokens.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");

        var result = await CreateHandler().Handle(new LoginCommand("alice", "pw"), CancellationToken.None);

        result.UserId.Should().Be(user.Id);
        result.Username.Should().Be("alice");
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_ValidCredentials_UpdatesLoginTimestampsAndSaves()
    {
        var user = TestData.User(passwordHash: "stored-hash");
        _users.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns(new AccessToken("t", DateTime.UtcNow));

        var before = DateTime.UtcNow;
        await CreateHandler().Handle(new LoginCommand("alice", "pw"), CancellationToken.None);

        user.LastLoginAt.Should().BeOnOrAfter(before);
        user.LastSeen.Should().BeOnOrAfter(before);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsUnauthorized()
    {
        _users.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new LoginCommand("ghost", "pw"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorized()
    {
        var user = TestData.User(passwordHash: "stored-hash");
        _users.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var act = () => CreateHandler().Handle(new LoginCommand("alice", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorized()
    {
        var user = TestData.User(passwordHash: "stored-hash", isActive: false);
        _users.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var act = () => CreateHandler().Handle(new LoginCommand("alice", "pw"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*deactivated*");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
