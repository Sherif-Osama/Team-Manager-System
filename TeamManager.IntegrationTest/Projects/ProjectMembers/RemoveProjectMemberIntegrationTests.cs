using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectMembers
{
    [Collection("Projects")]
    public sealed class RemoveProjectMemberIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid ProjectId, long MemberRowId, Guid OwnerId, string OwnerToken)> CreateProjectWithMemberAsync(
            string prefix)
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-member");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);

            return (projectId, memberRowId, ownerId, ownerToken);
        }

        [Fact]
        public async Task RemoveProjectMember_AsProjectOwner_SoftensRowAndDropsCount()
        {
            var (projectId, memberRowId, ownerId, ownerToken) = await CreateProjectWithMemberAsync("removemember-owner");

            await _helpers.RemoveProjectMemberAsync(projectId, memberRowId, ownerToken);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var membership = await db.ProjectMembers.AsNoTracking().SingleAsync(m => m.Id == memberRowId);

            Assert.Equal(ProjectMemberStatus.Removed, membership.Status);
            Assert.NotNull(membership.RemovedAtUtc);
            Assert.Equal(ownerId, membership.RemovedBy);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(1, project.MembersCount);
        }

        [Fact]
        public async Task RemoveProjectMember_AsProjectAdmin_Returns204()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("removemember-admin-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("removemember-admin");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("removemember-target");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, adminId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            await _helpers.AddProjectMemberAsync(projectId, adminId, ProjectRole.Admin, ownerToken);
            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Viewer, adminToken);

            await _helpers.RemoveProjectMemberAsync(projectId, memberRowId, adminToken);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(2, project.MembersCount);
        }

        [Fact]
        public async Task RemoveProjectMember_ForTheProjectOwnerMembership_Returns400()
        {
            var (projectId, _, ownerId, ownerToken) = await CreateProjectWithMemberAsync("removemember-ownerrow");

            var ownerRowId = await _helpers.GetProjectMemberIdAsync(projectId, ownerId);

            var response = await _helpers.RemoveProjectMemberRequestAsync(projectId, ownerRowId, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveProjectMember_ForUnknownMemberRow_Returns400()
        {
            var (projectId, _, _, ownerToken) = await CreateProjectWithMemberAsync("removemember-unknownrow");

            var response = await _helpers.RemoveProjectMemberRequestAsync(projectId, long.MaxValue, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task RemoveProjectMember_ForAlreadyRemovedMember_Returns400()
        {
            var (projectId, memberRowId, _, ownerToken) = await CreateProjectWithMemberAsync("removemember-twice");

            await _helpers.RemoveProjectMemberAsync(projectId, memberRowId, ownerToken);

            var response = await _helpers.RemoveProjectMemberRequestAsync(projectId, memberRowId, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveProjectMember_ForMembershipOfAnotherProject_Returns400()
        {
            var (projectId, memberRowId, _, ownerToken) = await CreateProjectWithMemberAsync("removemember-crossproject");

            var (_, otherProjectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var response = await _helpers.RemoveProjectMemberRequestAsync(otherProjectId, memberRowId, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(2, project.MembersCount);
        }

        [Fact]
        public async Task RemoveProjectMember_AsProjectMember_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("removemember-selfowner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("removemember-self");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);

            var response = await _helpers.RemoveProjectMemberRequestAsync(projectId, memberRowId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RemoveProjectMember_WithoutAccessToken_Returns401()
        {
            var (projectId, memberRowId, _, _) = await CreateProjectWithMemberAsync("removemember-notoken");

            var response = await _helpers.RemoveProjectMemberRequestAsync(projectId, memberRowId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RemoveProjectMember_ForUnknownProject_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("removemember-badproject");

            var response = await _helpers.RemoveProjectMemberRequestAsync(Guid.NewGuid(), 1, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveProjectMember_AfterProjectIsDeleted_Returns404()
        {
            var (projectId, memberRowId, _, ownerToken) = await CreateProjectWithMemberAsync("removemember-deleted");

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.RemoveProjectMemberRequestAsync(projectId, memberRowId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
