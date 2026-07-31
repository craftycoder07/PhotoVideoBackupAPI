using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Media.GetThumbnail;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Media;

public class GetThumbnailHandlerTests
{
    private readonly Mock<IMediaRepository> _media = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IThumbnailService> _thumbnails = new();

    private GetThumbnailHandler CreateHandler() => new(_media.Object, _uow.Object, _thumbnails.Object);

    [Fact]
    public async Task Handle_ExistingThumbnail_ReturnsBytesWithoutGenerating()
    {
        var item = TestData.MediaItem(thumbnailPath: "/thumbs/abc_thumb.jpg");
        _media.Setup(m => m.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _thumbnails.Setup(t => t.GetBytesAsync("/thumbs/abc_thumb.jpg")).ReturnsAsync(new byte[] { 1, 2, 3 });

        var result = await CreateHandler().Handle(new GetThumbnailQuery(item.Id), CancellationToken.None);

        result.Should().Equal(1, 2, 3);
        _thumbnails.Verify(t => t.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaType>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingThumbnail_GeneratesPersistsPathAndReturnsBytes()
    {
        var item = TestData.MediaItem(thumbnailPath: null, serverPath: "/storage/alice/abc.jpg", type: MediaType.Photo);
        _media.Setup(m => m.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _thumbnails.Setup(t => t.GenerateAsync(item.Id, "/storage/alice/abc.jpg", MediaType.Photo))
            .ReturnsAsync("/thumbs/generated.jpg");
        _thumbnails.Setup(t => t.GetBytesAsync("/thumbs/generated.jpg")).ReturnsAsync(new byte[] { 9 });

        var result = await CreateHandler().Handle(new GetThumbnailQuery(item.Id), CancellationToken.None);

        result.Should().Equal(9);
        item.ThumbnailPath.Should().Be("/thumbs/generated.jpg");
        _thumbnails.Verify(t => t.GenerateAsync(item.Id, "/storage/alice/abc.jpg", MediaType.Photo), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingItem_ThrowsMediaItemNotFound()
    {
        _media.Setup(m => m.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        var act = () => CreateHandler().Handle(new GetThumbnailQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<MediaItemNotFoundException>();
    }
}
