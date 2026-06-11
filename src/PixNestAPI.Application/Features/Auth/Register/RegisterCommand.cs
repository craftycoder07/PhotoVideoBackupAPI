using MediatR;
using PixNestAPI.Application.Features.Auth.Dtos;

namespace PixNestAPI.Application.Features.Auth.Register;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<AuthResponseDto>;
