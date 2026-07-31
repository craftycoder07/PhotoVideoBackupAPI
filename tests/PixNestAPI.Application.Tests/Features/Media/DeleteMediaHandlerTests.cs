using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Media.Delete;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;

namespace PixNestAPI.Application.Tests.Features.Media;

public class DeleteMediaHandlerTests
{
    private readonly Mock<IMediaRepository> _media = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IFileStorageService> _storage = new();

    private DeleteMediaHandler CreateHandler() =>
        new(_media.Object, _uow.Object, _storage.Object);

    [Fact]
    public async Task Handle_ExistingItem_RemovesRowSavesAndDeletesFile()
    {
        var item = TestData.MediaItem(serverPath: "/storage/alice/abc.jpg");
        _media.Setup(m => m.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await CreateHandler().Handle(new DeleteMediaCommand(item.Id), CancellationToken.None);

        result.Should().BeTrue();
        _media.Verify(m => m.Remove(item), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.DeleteAsync("/storage/alice/abc.jpg"), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemWithThumbnail_DeletesThumbnailToo()
    {
        var item = TestData.MediaItem(serverPath: "/storage/alice/abc.jpg", thumbnailPath: "/storage/Thumbnails/abc_thumb.jpg");
        _media.Setup(m => m.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        await CreateHandler().Handle(new DeleteMediaCommand(item.Id), CancellationToken.None);

        _storage.Verify(s => s.DeleteAsync("/storage/alice/abc.jpg"), Times.Once);
        _storage.Verify(s => s.DeleteAsync("/storage/Thumbnails/abc_thumb.jpg"), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingItem_ReturnsFalseAndDoesNothing()
    {
        _media.Setup(m => m.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        var result = await CreateHandler().Handle(new DeleteMediaCommand("missing"), CancellationToken.None);

        result.Should().BeFalse();
        _media.Verify(m => m.Remove(It.IsAny<MediaItem>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}
