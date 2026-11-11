using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectMembers
{
    [Collection("Projects")]
    public sealed class AddProjectMemberIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task AddProjectMember_AsProjectOwner_ReturnsRowIdAndCountsTheMember()
        {
            var (projectId, userId, ownerToken) = await _helpers.CreateProjectWithTeamMemberAsync("addmember-owner");

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, userId, ProjectRole.Member, ownerToken);

            Assert.True(memberRowId > 0);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(2, project.MembersCount);
        }

        [Fact]
        public async Task AddProjectMember_AsProjectAdmin_ReturnsRowId()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-admin-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-admin");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-target");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, adminId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, targetId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            await _helpers.AddProjectMemberAsync(projectId, adminId, ProjectRole.Admin, ownerToken);

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, targetId, ProjectRole.Viewer, adminToken);

            Assert.True(memberRowId > 0);
        }

        [Fact]
        public async Task AddProjectMember_AsTeamOwnerWithoutProjectMembership_ReturnsRowId()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-teamowner-owner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-teamowner");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-teamowner-target");

            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);
            var (_, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken, teamId: teamId);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, targetId, TeamRole.Member, teamOwnerToken)).IsSuccessStatusCode);

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, targetId, ProjectRole.Member, teamOwnerToken);

            Assert.True(memberRowId > 0);
        }

        [Fact]
        public async Task AddProjectMember_AsProjectMemberOrViewer_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-lowroles-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-lowroles-member");
            var (viewerId, _, viewerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-lowroles-viewer");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-lowroles-target");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, viewerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, targetId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);
            await _helpers.AddProjectMemberAsync(projectId, viewerId, ProjectRole.Viewer, ownerToken);

            var asMember = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = targetId, projectRole = ProjectRole.Member }, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);

            var asViewer = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = targetId, projectRole = ProjectRole.Member }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_AsUserOutsideTheProject_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-outsider-owner");
            var (outsiderId, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-outsider");

            var (_, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = outsiderId, projectRole = ProjectRole.Member }, outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_WithoutAccessToken_Returns401()
        {
            var (projectId, userId, _) = await _helpers.CreateProjectWithTeamMemberAsync("addmember-notoken");

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId, projectRole = ProjectRole.Member });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_ForUnknownProject_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-badproject");

            var response = await _helpers.AddProjectMemberRequestAsync(Guid.NewGuid(),
                new { userId = Guid.NewGuid(), projectRole = ProjectRole.Member }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_ForDeletedProject_Returns404()
        {
            var (projectId, userId, ownerToken) = await _helpers.CreateProjectWithTeamMemberAsync("addmember-deleted");

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId, projectRole = ProjectRole.Member }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_ForUnknownUser_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-baduser");
            var (_, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = Guid.NewGuid(), projectRole = ProjectRole.Member }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_ForUserOutsideTheProjectTeam_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-noteamteam-owner");
            var (strangerId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-noteamteam-stranger");

            var (_, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId = strangerId, projectRole = ProjectRole.Member }, ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_ForUserWhoLeftTheTeam_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-leftteam-owner");
            var (userId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("addmember-leftteam-target");

            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var teamMemberRowId = await _teamHelpers.AddMemberReturningIdAsync(teamId, userId, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent,
                (await _teamHelpers.RemoveMemberAsync(teamId, teamMemberRowId, ownerToken)).StatusCode);

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId, projectRole = ProjectRole.Member }, ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_ForAlreadyActiveMember_Returns400()
        {
            var (projectId, userId, ownerToken) = await _helpers.CreateProjectWithTeamMemberAsync("addmember-duplicate");

            await _helpers.AddProjectMemberAsync(projectId, userId, ProjectRole.Member, ownerToken);

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId, projectRole = ProjectRole.Viewer }, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddProjectMember_WithOwnerRole_Returns400()
        {
            var (projectId, userId, ownerToken) = await _helpers.CreateProjectWithTeamMemberAsync("addmember-ownerrole");

            var response = await _helpers.AddProjectMemberRequestAsync(projectId,
                new { userId, projectRole = ProjectRole.Owner }, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task AddProjectMember_ForPreviouslyRemovedUser_CreatesFreshMembership()
        {
            var (projectId, userId, ownerToken) = await _helpers.CreateProjectWithTeamMemberAsync("addmember-rejoin");

            var firstRowId = await _helpers.AddProjectMemberAsync(projectId, userId, ProjectRole.Member, ownerToken);
            await _helpers.RemoveProjectMemberAsync(projectId, firstRowId, ownerToken);

            var secondRowId = await _helpers.AddProjectMemberAsync(projectId, userId, ProjectRole.Viewer, ownerToken);

            Assert.True(secondRowId > firstRowId);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(2, project.MembersCount);
        }
    }
}
