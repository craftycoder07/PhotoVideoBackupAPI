using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Sessions.Start;

public record StartSessionCommand(string UserId, BackupSessionInfo SessionInfo) : IRequest<BackupSessionDto>;
