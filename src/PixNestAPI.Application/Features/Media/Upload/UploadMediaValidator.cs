using FluentValidation;

namespace PixNestAPI.Application.Features.Media.Upload;

public class UploadMediaValidator : AbstractValidator<UploadMediaCommand>
{
    private static readonly long MaxFileSize = 100 * 1024 * 1024; // 100 MB

    public UploadMediaValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize).WithMessage("File size exceeds the 100 MB limit.");
    }
}
