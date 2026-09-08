using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserByEmail;

namespace TeamManager.Tests.Users.AdminUserManagement.GetUserByEmailTests;

public sealed class GetUserByEmailQueryValidatorTests
{
    private readonly GetUserByEmailQueryValidator _validator = new();

    [Fact]
    public void Validate_WithEmail_IsValid() => Assert.True(_validator.Validate(new GetUserByEmailQuery("user@example.com")).IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_IsInvalid(string? email) => Assert.False(_validator.Validate(new GetUserByEmailQuery(email!)).IsValid);
}
