using FluentAssertions;
using PixNestAPI.Application.Features.Auth.Login;

namespace PixNestAPI.Application.Tests.Features.Auth;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new LoginCommand("alice", "password"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("alice", "")]
    [InlineData("", "")]
    public void Validate_EmptyFields_Fails(string username, string password)
    {
        var result = _validator.Validate(new LoginCommand(username, password));
        result.IsValid.Should().BeFalse();
    }
}
