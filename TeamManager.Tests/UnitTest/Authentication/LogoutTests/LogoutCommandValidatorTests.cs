using TeamManager.Application.Features.Authentication.Commands.Logout;

namespace TeamManager.Tests.UnitTest.Authentication.LogoutTests;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public void Validate_WithRefreshToken_IsValid() => Assert.True(_validator.Validate(new LogoutCommand("token")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankRefreshToken_IsInvalid(string? token) => Assert.False(_validator.Validate(new LogoutCommand(token!)).IsValid);
}