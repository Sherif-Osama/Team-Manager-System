using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using TeamManager.Domain.Entities;
using TeamManager.Infrastructure.Services.AuthenticationServices;

namespace TeamManager.Tests.UnitTest.Infrastructure.AuthenticationServices;

public sealed class AccessTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        Issuer = "TeamManager.Api",
        Audience = "TeamManager.Client",
        ExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private readonly AccessTokenService _sut = new(Microsoft.Extensions.Options.Options.Create(Options));

    private static User CreateUser(Guid? id = null)
    {
        var user = new User(id ?? Guid.NewGuid(), "user@example.com", "Test User", "password-hash", roleId: 1);

        SetRole(user.UserRoles.First(), new Role("User"));
        return user;
    }

    private static void SetRole(UserRole userRole, Role role)
    {
        typeof(UserRole).GetProperty(nameof(UserRole.Role),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(userRole, role);
    }

    [Fact]
    public void GenerateAccessToken_IncludesIdentityClaims()
    {
        var user = CreateUser();
        var token = _sut.GenerateAccessToken(user);
        var claims = ReadClaims(token);

        Assert.Equal(user.Id.ToString(), claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(user.Email, claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value);
        Assert.Equal(user.DisplayName, claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);
    }

    [Fact]
    public void GenerateAccessToken_IncludesRoleClaimPerUserRole()
    {
        var user = CreateUser();
        user.AssignRole(2);
        foreach (var userRole in user.UserRoles)
            SetRole(userRole, new Role(userRole.RoleId == 1 ? "User" : "SystemAdmin"));

        var token = _sut.GenerateAccessToken(user);
        var roleClaims = ReadClaims(token).Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        Assert.Contains("User", roleClaims);
        Assert.Contains("SystemAdmin", roleClaims);
    }

    [Fact]
    public void GenerateAccessToken_SetsIssuerAudienceAndExpiry()
    {
        var token = _sut.GenerateAccessToken(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Options.Issuer, jwt.Issuer);
        Assert.Equal(Options.Audience, jwt.Audiences.Single());
        Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void GenerateAccessToken_ProducesTokenValidAgainstTheSigningKey()
    {
        var user = CreateUser();
        var token = _sut.GenerateAccessToken(user);

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Options.Issuer,
            ValidateAudience = true,
            ValidAudience = Options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Key))
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    private static IEnumerable<Claim> ReadClaims(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
}
