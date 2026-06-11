using MediatR;

namespace PixNestAPI.Application.Features.Media.GetThumbnail;

public record GetThumbnailQuery(string MediaId) : IRequest<byte[]>;
