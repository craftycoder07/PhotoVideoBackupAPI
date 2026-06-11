using MediatR;

namespace PixNestAPI.Application.Features.Media.Delete;

public record DeleteMediaCommand(string MediaId) : IRequest<bool>;
