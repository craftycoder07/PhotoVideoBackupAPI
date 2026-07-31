using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Media.GetItem;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Media;

public class GetMediaItemHandlerTests
{
    private readonly Mock<IMediaRepository> _media = new();

    private GetMediaItemHandler CreateHandler() => new(_media.Object);

    [Fact]
    public async Task Handle_ExistingItem_ReturnsMappedDto()
    {
        var item = TestData.MediaItem(fileName: "vacation.jpg");
        item.FileExtension = ".jpg";
        item.FileSize = 4096;
        _media.Setup(m => m.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await CreateHandler().Handle(new GetMediaItemQuery(item.Id), CancellationToken.None);

        result.Id.Should().Be(item.Id);
        result.FileName.Should().Be("vacation.jpg");
        result.FileExtension.Should().Be(".jpg");
        result.FileSize.Should().Be(4096);
        result.Type.Should().Be(item.Type);
    }

    [Fact]
    public async Task Handle_MissingItem_ThrowsMediaItemNotFound()
    {
        _media.Setup(m => m.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        var act = () => CreateHandler().Handle(new GetMediaItemQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<MediaItemNotFoundException>();
    }
}
