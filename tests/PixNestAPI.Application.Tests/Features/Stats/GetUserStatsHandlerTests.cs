using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Stats.GetUserStats;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Stats;

public class GetUserStatsHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    private GetUserStatsHandler CreateHandler() => new(_users.Object);

    [Fact]
    public async Task Handle_ExistingUser_ReturnsUserStats()
    {
        var user = TestData.User();
        user.Stats.TotalPhotos = 7;
        user.Stats.TotalVideos = 3;
        user.Stats.TotalSize = 123456;
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetUserStatsQuery(user.Id), CancellationToken.None);

        result.Should().BeSameAs(user.Stats);
        result.TotalPhotos.Should().Be(7);
        result.TotalVideos.Should().Be(3);
        result.TotalSize.Should().Be(123456);
    }

    [Fact]
    public async Task Handle_MissingUser_ThrowsUserNotFound()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetUserStatsQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
