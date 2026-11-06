using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Users")]
    public sealed class AccountRaceConditionsIntegrationTests(UsersFixture factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task DeleteMyAccount_WhenTwoAdminsDeleteConcurrently_DeletesExactlyOneAccountAndRejectsTheOther()
        {
            await _helpers.SoftDeleteAllActiveSystemAdminsAsync();

            var (firstAdminId, firstAdminToken) = await _authenticatedUser.CreateAdminAsync("race-admin-first");
            var (secondAdminId, secondAdminToken) = await _authenticatedUser.CreateAdminAsync("race-admin-second");

            var firstDelete = _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" },
                firstAdminToken);
            var secondDelete = _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" },
                secondAdminToken);

            var responses = await Task.WhenAll(firstDelete, secondDelete);

            var statuses = responses.Select(r => r.StatusCode).OrderBy(s => s).ToArray();

            // Exactly one account is deleted; the system rejects the other so it is never left without administrators.
            Assert.Equal(HttpStatusCode.NoContent, statuses[0]);
            Assert.Equal(HttpStatusCode.Forbidden, statuses[1]);

            var rejected = responses.Single(r => r.StatusCode == HttpStatusCode.Forbidden);
            await ProblemDetailsAssertions.AssertDetailAsync(rejected, HttpStatusCode.Forbidden,
                "last system administrator cannot delete");

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var deletedAdminId = responses[0].StatusCode == HttpStatusCode.NoContent ? firstAdminId : secondAdminId;
            var survivingAdminId = deletedAdminId == firstAdminId ? secondAdminId : firstAdminId;

            Assert.NotNull((await db.Users.AsNoTracking().SingleAsync(u => u.Id == deletedAdminId)).DeletedAtUtc);

            var survivingAdmin = await db.Users.AsNoTracking().SingleAsync(u => u.Id == survivingAdminId);
            Assert.Null(survivingAdmin.DeletedAtUtc);
            Assert.True(survivingAdmin.IsActive);

            var activeAdminCount = await db.Users.CountAsync(u => u.DeletedAtUtc == null && u.IsActive
                && u.UserRoles.Any(ur => ur.Role.Name == DefaultRoles.Admin));

            Assert.Equal(1, activeAdminCount);
        }

        [Fact]
        public async Task TransferOwnership_WhileTheNewOwnerDeletesTheirAccount_OneOperationSucceedsAndTheTeamIsNeverOwnedByADeletedUser()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("race-transfer-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("race-transfer-member");

            var teamId = await _teamHelpers.CreateTeamAsync("Race Transfer Team", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var transfer = _teamHelpers.TransferOwnershipAsync(teamId, memberId, ownerToken);
            var delete = _helpers.DeleteMyAccountRequestAsync(new { currentPassword = "P@ssword1234" }, memberToken);

            var responses = await Task.WhenAll(transfer, delete);

            // Exactly one operation wins; the loser is rejected depending on which one committed first.
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
            Assert.Single(responses, r => r.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team2 = await db2.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);
            var member = await db2.Users.AsNoTracking().SingleAsync(u => u.Id == memberId);

            if (team2.OwnerUserId == memberId)
            {
                // The transfer won: the account deletion must have been rejected and the new owner must be intact.
                Assert.Equal(HttpStatusCode.Conflict, responses[1].StatusCode);
                Assert.Null(member.DeletedAtUtc);
                Assert.True(member.IsActive);

                var membership = await db2.TeamMembers.AsNoTracking()
                    .SingleAsync(m => m.TeamId == teamId && m.UserId == memberId);

                Assert.Equal(TeamRole.Owner, membership.TeamRole);
            }
            else
            {
                // The deletion won: the transfer must have been rejected and the team keeps its original owner.
                Assert.Equal(HttpStatusCode.NotFound, responses[0].StatusCode);
                Assert.Equal(ownerId, team2.OwnerUserId);
                Assert.NotNull(member.DeletedAtUtc);

                var memberships = await db2.TeamMembers.AsNoTracking()
                    .Where(m => m.TeamId == teamId && m.UserId == memberId).ToListAsync();

                Assert.DoesNotContain(memberships, m => m.Status == TeamMemberStatus.Active);
            }
        }
    }
}
