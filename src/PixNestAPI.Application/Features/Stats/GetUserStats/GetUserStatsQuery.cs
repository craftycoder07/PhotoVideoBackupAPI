using MediatR;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Stats.GetUserStats;

public record GetUserStatsQuery(string UserId) : IRequest<BackupStats>;
