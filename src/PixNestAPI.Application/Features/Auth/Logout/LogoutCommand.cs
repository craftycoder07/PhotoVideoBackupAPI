using MediatR;

namespace PixNestAPI.Application.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<bool>;
