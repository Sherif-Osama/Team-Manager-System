using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById;

namespace TeamManager.Tests.Users.AdminUserManagement.GetUserByIdTests;

public sealed class GetUserByIdQueryValidatorTests
{
    private readonly GetUserByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WithId_IsValid() => Assert.True(_validator.Validate(new GetUserByIdQuery(Guid.NewGuid())).IsValid);

    [Fact]
    public void Validate_WithEmptyId_IsInvalid() => Assert.False(_validator.Validate(new GetUserByIdQuery(Guid.Empty)).IsValid);
}
