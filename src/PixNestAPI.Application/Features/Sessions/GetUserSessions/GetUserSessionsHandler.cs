using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;
using PixNestAPI.Application.Interfaces.Repositories;

namespace PixNestAPI.Application.Features.Sessions.GetUserSessions;

public class GetUserSessionsHandler : IRequestHandler<GetUserSessionsQuery, List<BackupSessionDto>>
{
    private readonly ISessionRepository _sessions;

    public GetUserSessionsHandler(ISessionRepository sessions) => _sessions = sessions;

    public async Task<List<BackupSessionDto>> Handle(GetUserSessionsQuery request, CancellationToken ct)
    {
        var sessions = await _sessions.GetUserSessionsAsync(request.UserId, ct);

        return sessions.Select(s => new BackupSessionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Status = s.Status,
            TotalItems = s.TotalItems,
            ProcessedItems = s.ProcessedItems,
            SuccessfulBackups = s.SuccessfulBackups,
            FailedBackups = s.FailedBackups,
            SkippedItems = s.SkippedItems,
            TotalSize = s.TotalSize,
            ErrorMessage = s.ErrorMessage,
            Errors = s.Errors,
            SessionInfo = s.SessionInfo
        }).ToList();
    }
}
