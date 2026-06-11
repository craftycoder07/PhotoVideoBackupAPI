using MediatR;
using PixNestAPI.Application.Features.Users.Dtos;

namespace PixNestAPI.Application.Features.Users.GetUser;

public record GetUserQuery(string UserId) : IRequest<UserDto>;
