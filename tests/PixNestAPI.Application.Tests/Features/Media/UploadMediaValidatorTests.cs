using FluentAssertions;
using PixNestAPI.Application.Features.Media.Upload;

namespace PixNestAPI.Application.Tests.Features.Media;

public class UploadMediaValidatorTests
{
    private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB
    private readonly UploadMediaValidator _validator = new();

    private static UploadMediaCommand Command(string sessionId = "session-1", string fileName = "photo.jpg", long fileSize = 1024)
        => new(sessionId, Stream.Null, fileName, "image/jpeg", fileSize, null);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        _validator.Validate(Command()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptySessionId_Fails()
    {
        _validator.Validate(Command(sessionId: "")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyFileName_Fails()
    {
        _validator.Validate(Command(fileName: "")).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveFileSize_Fails(long fileSize)
    {
        _validator.Validate(Command(fileSize: fileSize)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_FileSizeAtLimit_Passes()
    {
        _validator.Validate(Command(fileSize: MaxFileSize)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_FileSizeOverLimit_FailsWithLimitMessage()
    {
        var result = _validator.Validate(Command(fileSize: MaxFileSize + 1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "File size exceeds the 100 MB limit.");
    }
}
