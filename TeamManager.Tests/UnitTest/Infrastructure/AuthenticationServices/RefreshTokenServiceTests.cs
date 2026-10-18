using Microsoft.Extensions.Options;
using TeamManager.Infrastructure.Services.AuthenticationServices;

namespace TeamManager.Tests.UnitTest.Infrastructure.AuthenticationServices;

public sealed class RefreshTokenServiceTests
{
    private readonly RefreshTokenService _sut = new(Options.Create(new JwtOptions
    {
        Key = "key",
        Issuer = "issuer",
        Audience = "audience",
        ExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    }));

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        Assert.False(string.IsNullOrEmpty(_sut.GenerateToken()));
    }

    [Fact]
    public void GenerateToken_ReturnsUniqueTokenEachCall()
    {
        var first = _sut.GenerateToken();
        var second = _sut.GenerateToken();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void HashToken_IsDeterministicAndHexEncoded()
    {
        var first = _sut.HashToken("raw-token");
        var second = _sut.HashToken("raw-token");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.Matches("^[0-9A-F]{64}$", first);
    }

    [Fact]
    public void GetExpiration_ReturnsConfiguredDaysAhead()
    {
        var before = DateTime.UtcNow.AddDays(6);
        var after = DateTime.UtcNow.AddDays(8);

        var expiration = _sut.GetExpiration();

        Assert.InRange(expiration, before, after);
    }
}
