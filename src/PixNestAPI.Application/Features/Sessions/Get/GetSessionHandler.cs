using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Features.Sessions.Get;

public class GetSessionHandler : IRequestHandler<GetSessionQuery, BackupSessionDto>
{
    private readonly ISessionRepository _sessions;

    public GetSessionHandler(ISessionRepository sessions) => _sessions = sessions;

    public async Task<BackupSessionDto> Handle(GetSessionQuery request, CancellationToken ct)
    {
        var session = await _sessions.GetByIdWithItemsAsync(request.SessionId, ct)
            ?? throw new SessionNotFoundException(request.SessionId);

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
