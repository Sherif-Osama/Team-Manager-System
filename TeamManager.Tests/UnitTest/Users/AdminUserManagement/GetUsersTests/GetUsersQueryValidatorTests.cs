using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUsers;

namespace TeamManager.Tests.UnitTest.Users.AdminUserManagement.GetUsersTests;

public sealed class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    public void Validate_WithPaginationBoundaries_IsValid(int page, int pageSize) =>
        Assert.True(_validator.Validate(new GetUsersQuery(null, null, null, page, pageSize)).IsValid);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public void Validate_WithInvalidPagination_IsInvalid(int page, int pageSize) =>
        Assert.False(_validator.Validate(new GetUsersQuery(null, null, null, page, pageSize)).IsValid);
}
