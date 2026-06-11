using MediatR;
using PixNestAPI.Application.Features.Auth.Dtos;

namespace PixNestAPI.Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    // Requires a refresh token store (DB table or Redis) to validate the token and look up the user.
    // Tracked as a future implementation task.
    public Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
        => throw new NotImplementedException("Refresh token validation requires a token store.");
}
