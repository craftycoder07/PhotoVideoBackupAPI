using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;

namespace PixNestAPI.Application.Features.Sessions.GetUserSessions;

public record GetUserSessionsQuery(string UserId) : IRequest<List<BackupSessionDto>>;
