using MediatR;
using PixNestAPI.Application.Features.Users.Dtos;
using PixNestAPI.Domain.ValueObjects;

namespace PixNestAPI.Application.Features.Users.UpdateSettings;

public record UpdateSettingsCommand(string UserId, DeviceSettings Settings) : IRequest<UserDto>;
