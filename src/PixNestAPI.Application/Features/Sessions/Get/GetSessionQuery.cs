using MediatR;
using PixNestAPI.Application.Features.Sessions.Dtos;

namespace PixNestAPI.Application.Features.Sessions.Get;

public record GetSessionQuery(string SessionId) : IRequest<BackupSessionDto>;
