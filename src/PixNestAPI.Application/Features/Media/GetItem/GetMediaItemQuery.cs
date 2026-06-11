using MediatR;
using PixNestAPI.Application.Features.Media.Dtos;

namespace PixNestAPI.Application.Features.Media.GetItem;

public record GetMediaItemQuery(string MediaId) : IRequest<MediaItemDto>;
