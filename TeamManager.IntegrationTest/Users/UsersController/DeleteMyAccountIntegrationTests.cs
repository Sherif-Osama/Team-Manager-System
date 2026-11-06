using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Authentication.Configuration;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Users")]
    public sealed class DeleteMyAccountIntegrationTests(UsersFixture factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly ProjectTestHelpers _projectHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task DeleteMyAccount_WithCorrectPassword_Returns204AndSoftDeletesTheAccount()
        {
            var (userId, email, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-valid");

            var response = await _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" }, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _helpers.GetMyProfileAsync(accessToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _helpers.RequestAsync(HttpMethod.Post,
                AuthenticationEndpoints.Login, body: new { email, password = "P@ssword1234" })).StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.NotNull(user.DeletedAtUtc);
            Assert.False(user.IsActive);
        }

        [Fact]
        public async Task DeleteMyAccount_WithWrongPassword_Returns403()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-wrongpassword");

            var response = await _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "WrongPassword123" },
                accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Forbidden, "Invalid password");
        }

        [Fact]
        public async Task DeleteMyAccount_AsTheLastSystemAdmin_Returns403()
        {
            await _helpers.SoftDeleteAllActiveSystemAdminsAsync();

            var (userId, accessToken) = await _authenticatedUser.CreateAdminAsync("delete-lastadmin");

            var response = await _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" }, accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Forbidden,
                "last system administrator cannot delete");

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Null((await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId)).DeletedAtUtc);
        }

        [Fact]
        public async Task DeleteMyAccount_AsATeamOwner_Returns409()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-teamowner");

            await _teamHelpers.CreateTeamAsync("Delete Owner Team", null, accessToken);

            var response = await _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" }, accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Conflict, "owns an active team");
        }

        [Fact]
        public async Task DeleteMyAccount_AsAProjectOwner_Returns409()
        {
            var (teamId, _, ownerToken) = await _projectHelpers.CreateOwnedProjectAsync("delete-projectowner");

            // The helper also creates the team that hosts the project; the account owner must
            // not own an active team for this test to exercise the project-ownership guard.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Conflict, "owns an active project");
        }

        [Fact]
        public async Task DeleteMyAccount_AsAMemberOfAnotherUsersTeam_RemovesTheMemberships()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-member-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-member");

            var teamId = await _teamHelpers.CreateTeamAsync("Delete Member Team", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" }, memberToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var membership = await db2.TeamMembers.AsNoTracking()
                .SingleAsync(m => m.TeamId == teamId && m.UserId == memberId);

            Assert.Equal(TeamMemberStatus.Removed, membership.Status);
            Assert.NotNull(membership.RemovedAtUtc);
        }

        [Fact]
        public async Task DeleteMyAccount_WithoutAccessToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" })).StatusCode);
        }

        [Fact]
        public async Task DeleteMyAccount_WithoutAPasswordBody_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-nopassword");

            var response = await _helpers.DeleteMyAccountRequestAsync(new { }, accessToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
