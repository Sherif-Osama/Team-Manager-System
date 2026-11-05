using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectMembers
{
    [Collection("Projects")]
    public sealed class ChangeMemberRoleIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid ProjectId, long MemberRowId, string OwnerToken)> CreateProjectWithMemberAsync(string prefix)
        {
            var (projectId, userId, ownerToken) = await _helpers.CreateProjectWithTeamMemberAsync(prefix);

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, userId, ProjectRole.Member, ownerToken);

            return (projectId, memberRowId, ownerToken);
        }

        private async Task<ProjectRole> GetStoredRoleAsync(long memberRowId)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var membership = await db.ProjectMembers.AsNoTracking().SingleAsync(m => m.Id == memberRowId);

            return membership.ProjectRole;
        }

        [Fact]
        public async Task ChangeMemberRole_AsProjectOwner_StoresTheNewRoleAndLogsIt()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-owner");

            await _helpers.ChangeMemberRoleAsync(projectId, memberRowId, ProjectRole.Admin, ownerToken);

            Assert.Equal(ProjectRole.Admin, await GetStoredRoleAsync(memberRowId));

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(2, project.MembersCount);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var activity = await db.ActivityLogs.AsNoTracking().SingleAsync(a =>
                a.ProjectId == projectId && a.ActivityType == ActivityTypes.ProjectMemberRoleChanged);

            Assert.Equal(EntityType.ProjectMember, activity.EntityType);
            Assert.Equal(memberRowId.ToString(), activity.EntityId);
            Assert.Contains("\"newRole\":\"Admin\"", activity.Metadata!);
        }

        [Fact]
        public async Task ChangeMemberRole_AsProjectAdmin_StoresTheNewRole()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-admin-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-admin");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-admin-target");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, adminId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            await _helpers.AddProjectMemberAsync(projectId, adminId, ProjectRole.Admin, ownerToken);
            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Viewer, ownerToken);

            await _helpers.ChangeMemberRoleAsync(projectId, memberRowId, ProjectRole.Member, adminToken);

            Assert.Equal(ProjectRole.Member, await GetStoredRoleAsync(memberRowId));
        }

        [Fact]
        public async Task ChangeMemberRole_PromotionDropsTheCachedMemberRole()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-promote-owner");
            var (promotedId, _, promotedToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-promoted");
            var (firstId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-promote-first");
            var (secondId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-promote-second");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, promotedId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, firstId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, secondId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var promotedRowId = await _helpers.AddProjectMemberAsync(projectId, promotedId, ProjectRole.Member, ownerToken);

            var before = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = firstId, projectRole = ProjectRole.Viewer }, promotedToken);

            Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

            await _helpers.ChangeMemberRoleAsync(projectId, promotedRowId, ProjectRole.Admin, ownerToken);

            // The behavior caches membership per project and user for five minutes, so this 200 is only
            // reachable if the role change evicted the cached UserAuthorization tag.
            var after = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = secondId, projectRole = ProjectRole.Viewer }, promotedToken);

            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_DemotionDropsTheCachedAdminRole()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-demote-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-demoted");
            var (firstId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-demote-first");
            var (secondId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-demote-second");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, adminId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, firstId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, secondId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var adminRowId = await _helpers.AddProjectMemberAsync(projectId, adminId, ProjectRole.Admin, ownerToken);

            await _helpers.AddProjectMemberAsync(projectId, firstId, ProjectRole.Viewer, adminToken);

            await _helpers.ChangeMemberRoleAsync(projectId, adminRowId, ProjectRole.Member, ownerToken);

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = secondId, projectRole = ProjectRole.Viewer }, adminToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_ToTheRoleTheMemberAlreadyHas_Returns400()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-samerole");

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, ProjectRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal(ProjectRole.Member, await GetStoredRoleAsync(memberRowId));
        }

        [Fact]
        public async Task ChangeMemberRole_ForTheProjectOwnerMembership_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-ownerrow");

            var (_, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var ownerRowId = await _helpers.GetProjectMemberIdAsync(projectId, ownerId);

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, ownerRowId, ProjectRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal(ProjectRole.Owner, await GetStoredRoleAsync(ownerRowId));
        }

        [Fact]
        public async Task ChangeMemberRole_ToOwnerRole_Returns400()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-ownerrole");

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, ProjectRole.Owner, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal(ProjectRole.Member, await GetStoredRoleAsync(memberRowId));
        }

        [Fact]
        public async Task ChangeMemberRole_ForUnknownMemberRow_Returns400()
        {
            var (projectId, _, ownerToken) = await CreateProjectWithMemberAsync("changerole-unknownrow");

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, long.MaxValue, ProjectRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_ForRemovedMembershipRow_Returns400()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-removedrow");

            await _helpers.RemoveProjectMemberAsync(projectId, memberRowId, ownerToken);

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, ProjectRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal(ProjectRole.Member, await GetStoredRoleAsync(memberRowId));
        }

        [Fact]
        public async Task ChangeMemberRole_ForMembershipOfAnotherProject_Returns400()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-crossproject");

            var (_, otherProjectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var response = await _helpers.ChangeMemberRoleRequestAsync(otherProjectId, memberRowId, ProjectRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal(ProjectRole.Member, await GetStoredRoleAsync(memberRowId));
        }

        [Fact]
        public async Task ChangeMemberRole_AsProjectMemberOrViewer_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-lowroles-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-lowroles-member");
            var (viewerId, _, viewerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-lowroles-viewer");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, viewerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);
            var viewerRowId = await _helpers.AddProjectMemberAsync(projectId, viewerId, ProjectRole.Viewer, ownerToken);

            var asMember = await _helpers.ChangeMemberRoleRequestAsync(projectId, viewerRowId, ProjectRole.Admin, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);

            var asViewer = await _helpers.ChangeMemberRoleRequestAsync(projectId, viewerRowId, ProjectRole.Admin, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_AsTeamOwnerWithoutProjectMembership_Returns204()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-teamowner-owner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-teamowner");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, teamOwnerId, ProjectRole.Viewer, ownerToken);

            await _helpers.ChangeMemberRoleAsync(projectId, memberRowId, ProjectRole.Member, teamOwnerToken);

            Assert.Equal(ProjectRole.Member, await GetStoredRoleAsync(memberRowId));
        }

        [Fact]
        public async Task ChangeMemberRole_AsUserOutsideTheProject_Returns403()
        {
            var (projectId, memberRowId, _) = await CreateProjectWithMemberAsync("changerole-outsider");

            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-outsider-stranger");

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, ProjectRole.Admin, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_WithoutAccessToken_Returns401()
        {
            var (projectId, memberRowId, _) = await CreateProjectWithMemberAsync("changerole-notoken");

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, ProjectRole.Admin);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_ForUnknownProject_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("changerole-badproject");

            var response = await _helpers.ChangeMemberRoleRequestAsync(Guid.NewGuid(), 1, ProjectRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_AfterProjectIsDeleted_Returns404()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-deleted");

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, ProjectRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_WithZeroMemberRow_Returns400WithFieldError()
        {
            var (projectId, _, ownerToken) = await CreateProjectWithMemberAsync("changerole-zerorow");

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, 0, ProjectRole.Admin, ownerToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "ProjectMemberId");
        }

        [Fact]
        public async Task ChangeMemberRole_WithEmptyProjectIdOrOutOfRangeRole_Returns400WithFieldError()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-badfields");

            var emptyProject = await _helpers.RequestAsync(HttpMethod.Put,
                ProjectEndpoints.MemberRole(Guid.Empty, memberRowId), ownerToken, ProjectRole.Admin);

            await ValidationAssertions.AssertFieldErrorAsync(emptyProject, "ProjectId");

            var unknownRole = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, (ProjectRole)9, ownerToken);

            await ValidationAssertions.AssertFieldErrorAsync(unknownRole, "Role");
        }

        [Fact]
        public async Task ChangeMemberRole_WithNonNumericRoleBody_Returns400()
        {
            var (projectId, memberRowId, ownerToken) = await CreateProjectWithMemberAsync("changerole-stringbody");

            var response = await _helpers.ChangeMemberRoleRequestAsync(projectId, memberRowId, "Admin", ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal(ProjectRole.Member, await GetStoredRoleAsync(memberRowId));
        }
    }
}
