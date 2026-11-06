using System.Net;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Users")]
    public sealed class UpdateMyProfileIntegrationTests(UsersFixture factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task UpdateMyProfile_WithValidDisplayName_Returns204AndPersistsTheChange()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("update-valid", "Old Name");

            await _helpers.UpdateMyProfileAsync(accessToken, "New Name");

            var profile = await _helpers.GetMyProfileResultAsync(accessToken);

            Assert.Equal("New Name", profile.DisplayName);
        }

        [Fact]
        public async Task UpdateMyProfile_WithEmptyDisplayName_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("update-empty");

            var response = await _helpers.UpdateMyProfileRequestAsync(new { displayName = "" }, accessToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "DisplayName");
        }

        [Fact]
        public async Task UpdateMyProfile_WithADisplayNameLongerThan100Characters_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("update-long");

            var response = await _helpers.UpdateMyProfileRequestAsync(new { displayName = new string('a', 101) }, accessToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "DisplayName");
        }

        [Fact]
        public async Task UpdateMyProfile_WithoutAccessToken_Returns401()
        {
            var response = await _helpers.UpdateMyProfileRequestAsync(new { displayName = "No Token" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
