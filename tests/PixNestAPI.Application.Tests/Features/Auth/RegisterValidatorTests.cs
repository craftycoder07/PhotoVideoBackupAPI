using FluentAssertions;
using PixNestAPI.Application.Features.Auth.Register;

namespace PixNestAPI.Application.Tests.Features.Auth;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    private static RegisterCommand Valid(string? username = null, string? email = null, string? password = null)
        => new(username ?? "alice", email ?? "alice@example.com", password ?? "password123");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]            // below MinimumLength(3)
    public void Validate_ShortOrEmptyUsername_Fails(string username)
    {
        _validator.Validate(Valid(username: username)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_UsernameOver50Chars_Fails()
    {
        _validator.Validate(Valid(username: new string('a', 51))).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_Fails(string email)
    {
        _validator.Validate(Valid(email: email)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]         // below MinimumLength(8)
    public void Validate_ShortPassword_Fails(string password)
    {
        _validator.Validate(Valid(password: password)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_PasswordOver100Chars_Fails()
    {
        _validator.Validate(Valid(password: new string('a', 101))).IsValid.Should().BeFalse();
    }
}
