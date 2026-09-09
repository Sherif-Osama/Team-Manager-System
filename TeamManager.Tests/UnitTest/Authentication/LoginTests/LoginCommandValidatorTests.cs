using TeamManager.Application.Features.Authentication.Commands.Login;

namespace TeamManager.Tests.UnitTest.Authentication.LoginTests;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidBoundaryValues_IsValid() =>
        Assert.True(_validator.Validate(new LoginCommand(new string('a', 244) + "@x.com", "12345678")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_IsInvalid(string? email) =>
        Assert.False(_validator.Validate(new LoginCommand(email!, "12345678")).IsValid);

    [Fact]
    public void Validate_WhenEmailExceedsMaximumLength_IsInvalid() =>
        Assert.False(_validator.Validate(new LoginCommand(new string('a', 257), "12345678")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567")]
    public void Validate_WithInvalidPassword_IsInvalid(string? password) =>
        Assert.False(_validator.Validate(new LoginCommand("user@example.com", password!)).IsValid);
}
