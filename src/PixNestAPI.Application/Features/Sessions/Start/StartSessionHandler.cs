using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;
using PixNestAPI.Application.Interfaces;
using PixNestAPI.Application.Interfaces.Repositories;
using PixNestAPI.Domain.Entities;
using PixNestAPI.Domain.Exceptions;

namespace PixNestAPI.Application.Features.Sessions.Start;

public class StartSessionHandler : IRequestHandler<StartSessionCommand, BackupSessionDto>
{
    private readonly ISessionRepository _sessions;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public StartSessionHandler(ISessionRepository sessions, IUserRepository users, IUnitOfWork uow)
    {
        _sessions = sessions;
        _users = users;
        _uow = uow;
    }

    public async Task<BackupSessionDto> Handle(StartSessionCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct)
            ?? throw new UserNotFoundException(request.UserId);

        var session = new BackupSession
        {
            UserId = request.UserId,
            SessionInfo = request.SessionInfo
        };

        _sessions.Add(session);
        user.LastSeen = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return ToDto(session);
    }

    private static BackupSessionDto ToDto(BackupSession s) => new()
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
    };
}
