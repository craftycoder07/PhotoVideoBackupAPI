using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;
using PixNestAPI.Domain.Enums;

namespace PixNestAPI.Application.Features.Sessions.Update;

public record UpdateSessionCommand(
    string SessionId,
    int? ProcessedItems,
    int? SuccessfulBackups,
    int? FailedBackups,
    int? SkippedItems,
    long? TotalSize,
    SessionStatus? Status,
    string? ErrorMessage
) : IRequest<BackupSessionDto>;
