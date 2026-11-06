using System.Net;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Users")]
    public sealed class DeactivateMyAccountIntegrationTests(UsersFixture factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task DeactivateMyAccount_WithCorrectPassword_Returns204AndDeactivatesTheAccount()
        {
            var (userId, email, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("deactivate-valid");

            var response = await _helpers.DeactivateMyAccountRequestAsync(new { currentPassword = "P@ssword1234" },
                accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var profile = await _helpers.GetMyProfileResultAsync(accessToken);

            Assert.Equal(userId, profile.Id);
            Assert.False(profile.IsActive);

            // Logging back in reactivates the account.
            var login = await _authenticatedUser.LoginAsync(email);

            Assert.NotNull(login.AccessToken);
            Assert.True((await _helpers.GetMyProfileResultAsync(accessToken)).IsActive);
        }

        [Fact]
        public async Task DeactivateMyAccount_WithWrongPassword_Returns403()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("deactivate-wrongpassword");

            var response = await _helpers.DeactivateMyAccountRequestAsync(new { currentPassword = "WrongPassword123" },
                accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Forbidden, "Invalid password");
        }

        [Fact]
        public async Task DeactivateMyAccount_AsTheLastSystemAdmin_Returns403()
        {
            await _helpers.SoftDeleteAllActiveSystemAdminsAsync();

            var (userId, accessToken) = await _authenticatedUser.CreateAdminAsync("deactivate-lastadmin");

            var response = await _helpers.DeactivateMyAccountRequestAsync(new { currentPassword = "P@ssword1234" },
                accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Forbidden,
                "last system administrator cannot deactivate");

            Assert.True((await _helpers.GetMyProfileResultAsync(accessToken)).IsActive);
        }

        [Fact]
        public async Task DeactivateMyAccount_WithoutAccessToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await _helpers.DeactivateMyAccountRequestAsync(new { currentPassword = "P@ssword1234" })).StatusCode);
        }

        [Fact]
        public async Task DeactivateMyAccount_WithoutAPasswordBody_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("deactivate-nopassword");

            var response = await _helpers.DeactivateMyAccountRequestAsync(new { }, accessToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
