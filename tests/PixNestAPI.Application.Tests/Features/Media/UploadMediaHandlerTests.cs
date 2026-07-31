using FluentAssertions;
using Moq;
using PixNestAPI.Application.Features.Media.Upload;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Application.Tests.Common;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Tests.Features.Media;

public class UploadMediaHandlerTests
{
    private readonly Mock<ISessionRepository> _sessions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMediaRepository> _media = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IFileStorageService> _storage = new();

    private UploadMediaHandler CreateHandler() =>
        new(_sessions.Object, _users.Object, _media.Object, _uow.Object, _storage.Object);

    private (BackupSession session, User user) ArrangeSessionAndUser()
    {
        var user = TestData.User(username: "alice");
        var session = TestData.Session(userId: user.Id);
        _sessions.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "alice", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SavedFile("/storage/alice/abc.jpg", "file-hash"));
        return (session, user);
    }

    private UploadMediaCommand Command(BackupSession session, string fileName, long fileSize = 2048)
        => new(session.Id, new MemoryStream(new byte[] { 1, 2, 3 }), fileName, "image/jpeg", fileSize, null);

    [Fact]
    public async Task Handle_PhotoExtension_CreatesPhotoAndIncrementsPhotoStats()
    {
        var (session, user) = ArrangeSessionAndUser();
        MediaItem? added = null;
        _media.Setup(m => m.Add(It.IsAny<MediaItem>())).Callback<MediaItem>(i => added = i);

        var result = await CreateHandler().Handle(Command(session, "vacation.jpg"), CancellationToken.None);

        added!.Type.Should().Be(MediaType.Photo);
        added.FileExtension.Should().Be(".jpg");
        added.Metadata.FileHash.Should().Be("file-hash");
        added.ServerPath.Should().Be("/storage/alice/abc.jpg");
        user.Stats.TotalPhotos.Should().Be(1);
        user.Stats.TotalVideos.Should().Be(0);
        result.Type.Should().Be(MediaType.Photo);
    }

    [Fact]
    public async Task Handle_NonPhotoExtension_CreatesVideoAndIncrementsVideoStats()
    {
        var (session, user) = ArrangeSessionAndUser();
        MediaItem? added = null;
        _media.Setup(m => m.Add(It.IsAny<MediaItem>())).Callback<MediaItem>(i => added = i);

        await CreateHandler().Handle(Command(session, "clip.mp4"), CancellationToken.None);

        added!.Type.Should().Be(MediaType.Video);
        user.Stats.TotalVideos.Should().Be(1);
        user.Stats.TotalPhotos.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Success_UpdatesSessionCountersAndUserStatsAndSaves()
    {
        var (session, user) = ArrangeSessionAndUser();

        await CreateHandler().Handle(Command(session, "vacation.jpg", fileSize: 5000), CancellationToken.None);

        session.TotalItems.Should().Be(1);
        session.SuccessfulBackups.Should().Be(1);
        session.TotalSize.Should().Be(5000);
        user.Stats.TotalSize.Should().Be(5000);
        user.Stats.SuccessfulBackups.Should().Be(1);
        user.Stats.LastBackupDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingSession_ThrowsSessionNotFound()
    {
        _sessions.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BackupSession?)null);

        var act = () => CreateHandler()
            .Handle(Command(TestData.Session(), "vacation.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<SessionNotFoundException>();
        _storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingUser_ThrowsUserNotFound()
    {
        var session = TestData.Session();
        _sessions.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _users.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(Command(session, "vacation.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
