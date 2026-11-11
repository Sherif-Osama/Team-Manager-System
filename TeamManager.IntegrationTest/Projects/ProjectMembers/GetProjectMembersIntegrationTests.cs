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
    public sealed class GetProjectMembersIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid TeamId, Guid ProjectId, Guid OwnerId, string OwnerToken)> CreateOwnedProjectAsync(string prefix)
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner");
            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            return (teamId, projectId, ownerId, ownerToken);
        }

        private async Task<(Guid UserId, long MemberRowId)> AddMemberToProjectAsync(Guid teamId, Guid projectId, ProjectRole role,
            string label, string ownerToken)
        {
            var (userId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync(label.Replace(' ', '-'), label);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, userId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var memberRowId = await _helpers.AddProjectMemberAsync(projectId, userId, role, ownerToken);

            return (userId, memberRowId);
        }

        [Fact]
        public async Task GetProjectMembers_AsOwner_ListsRosterWithJoinerFields()
        {
            var (teamId, projectId, ownerId, ownerToken) = await CreateOwnedProjectAsync("listmembers-owner");

            var (adminId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Admin, "Roster Admin", ownerToken);
            var (viewerId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Viewer, "Roster Viewer", ownerToken);

            var result = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Members.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);

            var admin = result.Members.Single(m => m.UserId == adminId);

            Assert.Equal("Roster Admin", admin.DisplayName);
            Assert.Equal(ProjectRole.Admin, admin.ProjectRole);
            Assert.Equal(ProjectMemberStatus.Active, admin.Status);
            Assert.Equal(ownerId, admin.AddedBy);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, admin.AddedByName);
            Assert.Null(admin.RemovedAtUtc);
            Assert.Null(admin.RemovedBy);
            Assert.Null(admin.RemovedByName);

            Assert.Contains(result.Members, m => m.UserId == ownerId && m.ProjectRole == ProjectRole.Owner);
            Assert.Contains(result.Members, m => m.UserId == viewerId && m.ProjectRole == ProjectRole.Viewer);
        }

        [Fact]
        public async Task GetProjectMembers_AsProjectMemberOrViewer_Returns200()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-lowroles");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("listmembers-lowroles-member");
            var (viewerId, _, viewerToken) = await _authenticatedUser.CreateConfirmedUserAsync("listmembers-lowroles-viewer");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            Assert.True((await _teamHelpers.AddMemberAsync(teamId, viewerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);
            await _helpers.AddProjectMemberAsync(projectId, viewerId, ProjectRole.Viewer, ownerToken);

            var asMember = await _helpers.GetProjectMembersResultAsync(projectId, memberToken);
            var asViewer = await _helpers.GetProjectMembersResultAsync(projectId, viewerToken);

            Assert.Equal(3, asMember.TotalCount);
            Assert.Equal(3, asViewer.TotalCount);
        }

        [Fact]
        public async Task GetProjectMembers_AsTeamOwnerWithoutProjectMembership_Returns200()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("listmembers-teamowner-user");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var roster = await _helpers.GetProjectMembersResultAsync(projectId, teamOwnerToken);

            Assert.Equal(1, roster.TotalCount);
            Assert.DoesNotContain(roster.Members, m => m.UserId == teamOwnerId);
        }

        [Fact]
        public async Task GetProjectMembers_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, _, _) = await CreateOwnedProjectAsync("listmembers-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("listmembers-stranger");

            var response = await _helpers.GetProjectMembersAsync(projectId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectMembers_WithoutAccessToken_Returns401()
        {
            var (_, projectId, _, _) = await CreateOwnedProjectAsync("listmembers-notoken");

            var response = await _helpers.GetProjectMembersAsync(projectId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectMembers_ForUnknownProject_Returns404()
        {
            var (_, _, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-badproject");

            var response = await _helpers.GetProjectMembersAsync(Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectMembers_AfterProjectIsDeleted_Returns404()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-deleted");

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.GetProjectMembersAsync(projectId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task GetProjectMembers_RouteProjectIdWinsOverTheQueryString()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-queryproject");
            var (_, otherProjectId) = await _helpers.CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            var (memberId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Roster Member", ownerToken);

            var result = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, $"projectId={otherProjectId}");

            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Members, m => m.UserId == memberId);
        }

        [Fact]
        public async Task GetProjectMembers_ListsRemovedMembersByDefault()
        {
            var (teamId, projectId, ownerId, ownerToken) = await CreateOwnedProjectAsync("listmembers-removed");
            var (firstId, firstRowId) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Roster First", ownerToken);
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Admin, "Roster Second", ownerToken);

            await _helpers.RemoveProjectMemberAsync(projectId, firstRowId, ownerToken);

            var result = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Members.Count(m => m.Status == ProjectMemberStatus.Active));

            var removed = result.Members.Single(m => m.Status == ProjectMemberStatus.Removed);

            Assert.Equal(firstId, removed.UserId);
            Assert.Equal(ownerId, removed.RemovedBy);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, removed.RemovedByName);
            Assert.NotNull(removed.RemovedAtUtc);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(2, project.MembersCount);
        }

        [Fact]
        public async Task GetProjectMembers_RoleFilterReturnsOnlyMatchingRows()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-rolefilter");
            var (adminId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Admin, "Roster Admin", ownerToken);
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Viewer, "Roster Viewer", ownerToken);

            var result = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, $"role={(int)ProjectRole.Admin}");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(adminId, result.Members.Single().UserId);
        }

        [Fact]
        public async Task GetProjectMembers_StatusFilterReturnsOnlyThatStatus()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-statusfilter");
            var (activeId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Roster Active", ownerToken);
            var (removedId, removedRowId) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member,
                "Roster Leaving", ownerToken);

            await _helpers.RemoveProjectMemberAsync(projectId, removedRowId, ownerToken);

            var result = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken,
                $"status={(int)ProjectMemberStatus.Removed}");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(removedId, result.Members.Single().UserId);

            var active = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken,
                $"status={(int)ProjectMemberStatus.Active}");

            Assert.Contains(active.Members, m => m.UserId == activeId);
            Assert.DoesNotContain(active.Members, m => m.UserId == removedId);
        }

        [Fact]
        public async Task GetProjectMembers_SearchIsTrimmedAndCaseInsensitive()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-search");
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Roster Alpha", ownerToken);
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Viewer, "Roster Beta", ownerToken);
            var (gammaId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Ops Gamma", ownerToken);

            var lowerCase = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "search=roster");

            Assert.Equal(2, lowerCase.TotalCount);
            Assert.DoesNotContain(lowerCase.Members, m => m.UserId == gammaId);

            var padded = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "search=%20Roster%20");

            Assert.Equal(2, padded.TotalCount);

            var blank = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "search=%20%20");

            Assert.Equal(4, blank.TotalCount);
        }

        [Fact]
        public async Task GetProjectMembers_SearchTreatsLikeWildcardsAsLiterals()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-wildcard");
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Roster Alpha", ownerToken);
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Viewer, "Ops Gamma", ownerToken);

            var percent = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "search=%25");

            Assert.Empty(percent.Members);
            Assert.Equal(0, percent.TotalCount);

            var underscore = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken,
                $"search={Uri.EscapeDataString("Ops_Gamm_")}");

            Assert.Empty(underscore.Members);
            Assert.Equal(0, underscore.TotalCount);
        }



        [Fact]
        public async Task GetProjectMembers_RoleFilterAcceptsNamesAndNumbers()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-stringrole");
            var (adminId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Admin, "Roster Admin", ownerToken);
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Viewer, "Roster Viewer", ownerToken);

            // Query binding parses enums with a TypeConverter, so names work here even though the JSON
            // request bodies of this API only accept the numeric value.
            var byName = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "role=Admin");

            Assert.Equal(1, byName.TotalCount);
            Assert.Equal(adminId, byName.Members.Single().UserId);

            var byNumber = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, $"role={(int)ProjectRole.Admin}");

            Assert.Equal(1, byNumber.TotalCount);

            var unknownName = await _helpers.GetProjectMembersAsync(projectId, ownerToken, "role=Superuser");

            await ValidationAssertions.AssertFieldErrorAsync(unknownName, "Role");
        }


        [Fact]
        public async Task GetProjectMembers_PagesNewestMembershipFirst()
        {
            var (teamId, projectId, ownerId, ownerToken) = await CreateOwnedProjectAsync("listmembers-paging");
            var (firstId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Roster First", ownerToken);
            var (secondId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Admin, "Roster Second", ownerToken);
            var (thirdId, _) = await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Viewer, "Roster Third", ownerToken);

            var all = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "pageSize=100");

            Assert.Equal(4, all.TotalCount);
            Assert.Equal(100, all.PageSize);
            Assert.Equal(new[] { thirdId, secondId, firstId, ownerId }, all.Members.Select(m => m.UserId));

            var pageOfOne = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "page=1&pageSize=1");

            Assert.Equal(4, pageOfOne.TotalCount);
            Assert.Equal(1, pageOfOne.Page);
            Assert.Equal(1, pageOfOne.PageSize);
            Assert.Equal(thirdId, pageOfOne.Members.Single().UserId);
        }

        [Fact]
        public async Task GetProjectMembers_BeyondTheLastPageReturnsNoRows()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("listmembers-lastpage");
            await AddMemberToProjectAsync(teamId, projectId, ProjectRole.Member, "Roster Member", ownerToken);

            var result = await _helpers.GetProjectMembersResultAsync(projectId, ownerToken, "page=9&pageSize=2");

            Assert.Empty(result.Members);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(9, result.Page);
            Assert.Equal(2, result.PageSize);
        }
    }
}
