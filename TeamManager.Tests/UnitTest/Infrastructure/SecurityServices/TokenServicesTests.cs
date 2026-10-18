using TeamManager.Infrastructure.Services.SecurityService;
using TeamManager.Infrastructure.Services.SecurityServices;

namespace TeamManager.Tests.UnitTest.Infrastructure.SecurityServices;

public sealed class TokenServicesTests
{
    [Fact]
    public void EmailConfirmationTokenService_GeneratesUniqueTokensAndStableHashes()
    {
        var service = new EmailConfirmationTokenService();

        var first = service.GenerateToken();
        var second = service.GenerateToken();

        Assert.False(string.IsNullOrEmpty(first));
        Assert.NotEqual(first, second);
        Assert.Equal(service.HashToken(first), service.HashToken(first));
        Assert.Matches("^[0-9A-F]{64}$", service.HashToken(first));
    }

    [Fact]
    public void InvitationTokenService_GeneratesUniqueTokensAndStableHashes()
    {
        var service = new InvitationTokenService();

        var first = service.GenerateToken();
        var second = service.GenerateToken();

        Assert.False(string.IsNullOrEmpty(first));
        Assert.NotEqual(first, second);
        Assert.Equal(service.HashToken(first), service.HashToken(first));
        Assert.Matches("^[0-9A-F]{64}$", service.HashToken(first));
    }
}
