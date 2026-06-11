using MediatR;
using PixNestAPI.Application.Features.Auth.Dtos;

namespace PixNestAPI.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
