using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Enums;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Features.Sessions.Update;

public class UpdateSessionHandler : IRequestHandler<UpdateSessionCommand, BackupSessionDto>
{
    private readonly ISessionRepository _sessions;
    private readonly IUnitOfWork _uow;

    public UpdateSessionHandler(ISessionRepository sessions, IUnitOfWork uow)
    {
        _sessions = sessions;
        _uow = uow;
    }

    public async Task<BackupSessionDto> Handle(UpdateSessionCommand request, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(request.SessionId, ct)
            ?? throw new SessionNotFoundException(request.SessionId);

        if (request.ProcessedItems.HasValue) session.ProcessedItems = request.ProcessedItems.Value;
        if (request.SuccessfulBackups.HasValue) session.SuccessfulBackups = request.SuccessfulBackups.Value;
        if (request.FailedBackups.HasValue) session.FailedBackups = request.FailedBackups.Value;
        if (request.SkippedItems.HasValue) session.SkippedItems = request.SkippedItems.Value;
        if (request.TotalSize.HasValue) session.TotalSize = request.TotalSize.Value;
        if (request.Status.HasValue) session.Status = request.Status.Value;
        if (!string.IsNullOrEmpty(request.ErrorMessage)) session.ErrorMessage = request.ErrorMessage;

        if (session.Status is SessionStatus.Completed or SessionStatus.Failed)
            session.EndTime = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        return new BackupSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Status = session.Status,
            TotalItems = session.TotalItems,
            ProcessedItems = session.ProcessedItems,
            SuccessfulBackups = session.SuccessfulBackups,
            FailedBackups = session.FailedBackups,
            SkippedItems = session.SkippedItems,
            TotalSize = session.TotalSize,
            ErrorMessage = session.ErrorMessage,
            Errors = session.Errors,
            SessionInfo = session.SessionInfo
        };
    }
}
