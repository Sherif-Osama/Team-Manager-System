using System.Net;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Teams.Configuration;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Common")]
    public sealed class GetMyProfileIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task GetMyProfile_AsAuthenticatedUser_ReturnsTheOwnProfile()
        {
            var (userId, email, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("profile-self", "Profile User");

            var profile = await _helpers.GetMyProfileResultAsync(accessToken);

            Assert.Equal(userId, profile.Id);
            Assert.Equal(email, profile.Email);
            Assert.Equal("Profile User", profile.DisplayName);
            Assert.True(profile.IsEmailConfirmed);
            Assert.True(profile.IsActive);
            Assert.NotNull(profile.LastLoginUtc);
            Assert.NotEqual(default, profile.CreatedAtUtc);
            Assert.Equal(0, profile.ActiveTeamMembersCount);
            Assert.Equal(0, profile.OwnedTeamsCount);
            Assert.Equal([DefaultRoles.User], profile.SystemRolesNames);
        }

        [Fact]
        public async Task GetMyProfile_ForAUserWithAnAdminRoleAndOwnedTeams_ReflectsTheCountsAndRoleNames()
        {
            var (userId, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("profile-admin");
            await _authenticatedUser.PromoteToAdminAsync(userId);
            await _teamHelpers.CreateTeamAsync("Profile Team", null, accessToken);

            var profile = await _helpers.GetMyProfileResultAsync(accessToken);

            Assert.Equal(1, profile.ActiveTeamMembersCount);
            Assert.Equal(1, profile.OwnedTeamsCount);
            Assert.Contains(DefaultRoles.Admin, profile.SystemRolesNames);
            Assert.Contains(DefaultRoles.User, profile.SystemRolesNames);
        }

        [Fact]
        public async Task GetMyProfile_ForADeletedUser_Returns404()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("profile-deleted");

            await _helpers.DeleteMyAccountAsync(accessToken, "P@ssword1234");

            Assert.Equal(HttpStatusCode.NotFound, (await _helpers.GetMyProfileAsync(accessToken)).StatusCode);
        }

        [Fact]
        public async Task GetMyProfile_WithoutAccessToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _helpers.GetMyProfileAsync()).StatusCode);
        }
    }
}
