using MediatR;
using PixNestAPI.Application.Features.Stats.Dtos;

namespace PixNestAPI.Application.Features.Stats.GetSystemStats;

public record GetSystemStatsQuery : IRequest<SystemStatsDto>;
