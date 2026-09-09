using TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole;

namespace TeamManager.Tests.UnitTest.Users.AdminUserManagement.RevokeRoleTests;

public sealed class RevokeRoleValidatorTests
{
    private readonly RevokeRoleValidator _validator = new();

    [Fact]
    public void Validate_WithValidIds_IsValid() => Assert.True(_validator.Validate(new RevokeRoleCommand(Guid.NewGuid(), 1)).IsValid);

    [Fact]
    public void Validate_WithEmptyUserId_IsInvalid() => Assert.False(_validator.Validate(new RevokeRoleCommand(Guid.Empty, 1)).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveRoleId_IsInvalid(int roleId) => Assert.False(_validator.Validate(new RevokeRoleCommand(Guid.NewGuid(), roleId)).IsValid);
}
