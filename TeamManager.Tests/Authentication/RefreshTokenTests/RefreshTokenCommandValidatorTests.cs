using TeamManager.Application.Features.Authentication.Commands.RefreshToken;

namespace TeamManager.Tests.Authentication.RefreshTokenTests;

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WithRefreshToken_IsValid() => Assert.True(_validator.Validate(new RefreshTokenCommand("token")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankRefreshToken_IsInvalid(string? token) =>
        Assert.False(_validator.Validate(new RefreshTokenCommand(token!)).IsValid);
}
