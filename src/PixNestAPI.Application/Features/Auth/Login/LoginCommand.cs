using MediatR;
using PixNestAPI.Application.Features.Auth.Dtos;

namespace PixNestAPI.Application.Features.Auth.Login;

public record LoginCommand(string Username, string Password) : IRequest<AuthResponseDto>;
