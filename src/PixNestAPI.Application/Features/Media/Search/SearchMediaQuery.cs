using MediatR;
using PixNestAPI.Application.Features.Media.Dtos;

namespace PixNestAPI.Application.Features.Media.Search;

public record SearchMediaQuery(
    string UserId,
    string Query,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<List<MediaItemDto>>;
