using TeamManager.Infrastructure.Services.AuthenticationServices;

namespace TeamManager.Tests.UnitTest.Infrastructure.AuthenticationServices;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ReturnsBcryptHash()
    {
        var hash = _sut.Hash("Password123!");

        Assert.False(string.IsNullOrEmpty(hash));
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Hash_ProducesDifferentHashForSamePasswordDueToSalt()
    {
        var first = _sut.Hash("Password123!");
        var second = _sut.Hash("Password123!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("Password123!");

        Assert.True(_sut.Verify("Password123!", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("Password123!");

        Assert.False(_sut.Verify("WrongPassword", hash));
    }
}
