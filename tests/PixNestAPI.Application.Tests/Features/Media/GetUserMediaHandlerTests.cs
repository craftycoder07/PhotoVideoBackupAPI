using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Media.GetUserMedia;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Tests.Features.Media;

public class GetUserMediaHandlerTests
{
    private readonly Mock<IMediaRepository> _media = new();

    private GetUserMediaHandler CreateHandler() => new(_media.Object);

    [Fact]
    public async Task Handle_MapsRepositoryItemsToDtosPreservingOrder()
    {
        var first = TestData.MediaItem(fileName: "a.jpg");
        var second = TestData.MediaItem(fileName: "b.mp4");
        _media.Setup(m => m.GetUserMediaAsync("user-1", 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaItem> { first, second });

        var result = await CreateHandler().Handle(new GetUserMediaQuery("user-1"), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(first.Id);
        result[1].Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task Handle_ForwardsPagingArguments()
    {
        _media.Setup(m => m.GetUserMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaItem>());

        await CreateHandler().Handle(new GetUserMediaQuery("user-1", Page: 3, PageSize: 25), CancellationToken.None);

        _media.Verify(m => m.GetUserMediaAsync("user-1", 3, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoItems_ReturnsEmptyList()
    {
        _media.Setup(m => m.GetUserMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaItem>());

        var result = await CreateHandler().Handle(new GetUserMediaQuery("user-1"), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
