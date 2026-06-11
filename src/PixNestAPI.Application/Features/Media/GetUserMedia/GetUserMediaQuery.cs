using MediatR;
using PixNestAPI.Application.Features.Media.Dtos;

namespace PixNestAPI.Application.Features.Media.GetUserMedia;

public record GetUserMediaQuery(string UserId, int Page = 1, int PageSize = 50) : IRequest<List<MediaItemDto>>;
