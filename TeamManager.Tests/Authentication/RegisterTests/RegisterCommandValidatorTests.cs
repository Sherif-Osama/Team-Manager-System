using TeamManager.Application.Features.Authentication.Commands.Register;

namespace TeamManager.Tests.Authentication.RegisterTests;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WithBoundaryValues_IsValid() =>
        Assert.True(_validator.Validate(new RegisterCommand(new string('a', 244) + "@x.com", new string('n', 100), "12345678")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_IsInvalid(string? email) =>
        Assert.False(_validator.Validate(new RegisterCommand(email!, "Display Name", "12345678")).IsValid);

    [Fact]
    public void Validate_WhenEmailExceedsMaximumLength_IsInvalid() =>
        Assert.False(_validator.Validate(new RegisterCommand(new string('a', 257), "Display Name", "12345678")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankDisplayName_IsInvalid(string? name) =>
        Assert.False(_validator.Validate(new RegisterCommand("user@example.com", name!, "12345678")).IsValid);

    [Fact]
    public void Validate_WhenDisplayNameExceedsMaximumLength_IsInvalid() =>
        Assert.False(_validator.Validate(new RegisterCommand("user@example.com", new string('n', 101), "12345678")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567")]
    public void Validate_WithInvalidPassword_IsInvalid(string? password) =>
        Assert.False(_validator.Validate(new RegisterCommand("user@example.com", "Display Name", password!)).IsValid);
}
