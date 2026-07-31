using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Users.GetUser;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Users;

public class GetUserHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    private GetUserHandler CreateHandler() => new(_users.Object);

    [Fact]
    public async Task Handle_ExistingUser_ReturnsMappedDto()
    {
        var user = TestData.User(username: "alice", email: "alice@example.com");
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetUserQuery(user.Id), CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Username.Should().Be("alice");
        result.Email.Should().Be("alice@example.com");
        result.IsActive.Should().BeTrue();
        result.Settings.Should().BeSameAs(user.Settings);
        result.Stats.Should().BeSameAs(user.Stats);
    }

    [Fact]
    public async Task Handle_MissingUser_ThrowsUserNotFound()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetUserQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
