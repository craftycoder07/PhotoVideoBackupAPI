using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Auth.Register;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Tests.Features.Auth;

public class RegisterHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();

    private RegisterHandler CreateHandler() =>
        new(_users.Object, _uow.Object, _hasher.Object, _tokens.Object);

    public RegisterHandlerTests()
    {
        // Default: name/email available, hasher and tokens return stub values.
        _users.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AccessToken("access-token", DateTime.UtcNow.AddHours(1)));
        _tokens.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
    }

    [Fact]
    public async Task Handle_NewUser_AddsHashedUserSavesAndReturnsTokens()
    {
        User? added = null;
        _users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => added = u);

        var result = await CreateHandler()
            .Handle(new RegisterCommand("alice", "alice@example.com", "password123"), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Username.Should().Be("alice");
        added.Email.Should().Be("alice@example.com");
        added.PasswordHash.Should().Be("hashed");
        _hasher.Verify(h => h.Hash("password123"), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Username.Should().Be("alice");
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsInvalidOperation()
    {
        _users.Setup(r => r.UsernameExistsAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler()
            .Handle(new RegisterCommand("alice", "alice@example.com", "password123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Username*");
        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperation()
    {
        _users.Setup(r => r.EmailExistsAsync("alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler()
            .Handle(new RegisterCommand("alice", "alice@example.com", "password123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Email*");
        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }
}
