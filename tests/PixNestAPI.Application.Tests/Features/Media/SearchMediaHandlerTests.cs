using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Media.Search;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Tests.Features.Media;

public class SearchMediaHandlerTests
{
    private readonly Mock<IMediaRepository> _media = new();

    private SearchMediaHandler CreateHandler() => new(_media.Object);

    [Fact]
    public async Task Handle_ForwardsAllSearchArgumentsToRepository()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 6, 1);
        _media.Setup(m => m.SearchAsync("user-1", "beach", from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaItem>());

        await CreateHandler().Handle(new SearchMediaQuery("user-1", "beach", from, to), CancellationToken.None);

        _media.Verify(m => m.SearchAsync("user-1", "beach", from, to, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsResultsToDtos()
    {
        var match = TestData.MediaItem(fileName: "beach.jpg");
        _media.Setup(m => m.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaItem> { match });

        var result = await CreateHandler().Handle(new SearchMediaQuery("user-1", "beach"), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(match.Id);
        result[0].FileName.Should().Be("beach.jpg");
    }

    [Fact]
    public async Task Handle_NullDates_PassedThroughAsNull()
    {
        _media.Setup(m => m.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaItem>());

        await CreateHandler().Handle(new SearchMediaQuery("user-1", "beach"), CancellationToken.None);

        _media.Verify(m => m.SearchAsync("user-1", "beach", null, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
