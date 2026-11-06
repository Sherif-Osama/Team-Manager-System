using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskChecklist
{
    [Collection("Projects")]
    public sealed class GetTaskChecklistIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task GetTaskChecklist_AsProjectViewer_ReturnsTheItemsOrderedBySortOrder()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Checklist Reader", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Checklist task", ownerToken);

            var firstItemId = await _helpers.AddChecklistItemAsync(taskId, "First", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Second", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Third", ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, viewerToken);

            Assert.Equal(new[] { "First", "Second", "Third" }, items.Select(x => x.Content).ToArray());
            Assert.Equal(new short[] { 0, 1, 2 }, items.Select(x => x.SortOrder).ToArray());
            Assert.Equal(firstItemId, items[0].Id);
            Assert.All(items, item => Assert.Equal(taskId, item.TaskId));
            Assert.All(items, item => Assert.False(item.IsCompleted));
        }

        [Fact]
        public async Task GetTaskChecklist_ForATaskWithoutItems_ReturnsAnEmptyArray()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-empty");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Empty checklist task", ownerToken);

            Assert.Empty(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));
        }

        [Fact]
        public async Task GetTaskChecklist_WithoutAProjectRole_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-roles");
            var (teamMemberId, _, teamMemberToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-get-teammember");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamMemberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-get-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Locked checklist task", ownerToken);

            var asTeamMember = await _helpers.GetTaskChecklistAsync(taskId, teamMemberToken);
            var asStranger = await _helpers.GetTaskChecklistAsync(taskId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asTeamMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asStranger.StatusCode);
        }

        [Fact]
        public async Task GetTaskChecklist_AsTeamOwnerWithoutProjectMembership_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-get-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner checklist task", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Visible to the team owner", ownerToken);

            Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, teamOwnerToken));
        }

        [Fact]
        public async Task GetTaskChecklist_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token checklist task", ownerToken);

            var response = await _helpers.GetTaskChecklistAsync(taskId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskChecklist_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-unknowntask");

            var response = await _helpers.GetTaskChecklistAsync(long.MaxValue, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskChecklist_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed checklist task", ownerToken);

            await _helpers.AddChecklistItemAsync(taskId, "Surviving row", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.GetTaskChecklistAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskChecklist_FilteredByIsCompleted_ReturnsTheMatchingSide()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-iscompleted");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Filter complete task", ownerToken);

            var openItemId = await _helpers.AddChecklistItemAsync(taskId, "Open", ownerToken);
            var firstDoneItemId = await _helpers.AddChecklistItemAsync(taskId, "Done one", ownerToken);
            var secondDoneItemId = await _helpers.AddChecklistItemAsync(taskId, "Done two", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, firstDoneItemId, ownerToken);
            await _helpers.CompleteChecklistItemAsync(taskId, secondDoneItemId, ownerToken);

            var completed = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken, "isCompleted=true");
            var open = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken, "isCompleted=false");

            Assert.Equal(new[] { firstDoneItemId, secondDoneItemId }, completed.Select(x => x.Id).ToArray());
            Assert.Equal(openItemId, Assert.Single(open).Id);
        }

        [Fact]
        public async Task GetTaskChecklist_FilteredBySearch_ReturnsTheMatchingItems()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-search");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Search task", ownerToken);

            await _helpers.AddChecklistItemAsync(taskId, "Write the release notes", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Review the release plan", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Water the plants", ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken, "search=RELEASE");

            Assert.Equal(new[] { "Write the release notes", "Review the release plan" },
                items.Select(x => x.Content).ToArray());
        }

        [Fact]
        public async Task GetTaskChecklist_TrimsTheSearchTerm()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-trimsearch");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Trim search task", ownerToken);

            await _helpers.AddChecklistItemAsync(taskId, "Ship the release", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Nothing here", ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken,
                $"search={Uri.EscapeDataString("  release  ")}");

            Assert.Equal("Ship the release", Assert.Single(items).Content);
        }

        [Fact]
        public async Task GetTaskChecklist_TreatsAWildcardInTheSearchLiterally()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-wildcard");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Wildcard search task", ownerToken);

            await _helpers.AddChecklistItemAsync(taskId, "100% done", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Twenty done", ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken,
                $"search={Uri.EscapeDataString("% done")}");

            Assert.Equal("100% done", Assert.Single(items).Content);
        }

        [Fact]
        public async Task GetTaskChecklist_CombinesSearchAndIsCompleted()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-combined");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Combined filters task", ownerToken);

            await _helpers.AddChecklistItemAsync(taskId, "Alpha plan", ownerToken);
            var doneItemId = await _helpers.AddChecklistItemAsync(taskId, "Alpha plan draft", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Beta plan", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, doneItemId, ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken, "search=alpha&isCompleted=true");

            Assert.Equal("Alpha plan draft", Assert.Single(items).Content);
        }

        [Fact]
        public async Task GetTaskChecklist_WithBlankFilters_ReturnsEveryItem()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-blankfilters");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Blank filters task", ownerToken);

            await _helpers.AddChecklistItemAsync(taskId, "One", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Two", ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken, "search=&isCompleted=");

            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task GetTaskChecklist_ExposesTheCompletingUser()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-get-completer");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Checklist Finisher", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Completer task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Completed by a member", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, itemId, memberToken);

            var item = Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));

            Assert.Equal(memberId, item.CompletedBy);
            Assert.Equal("Checklist Finisher", item.CompletedByName);
            Assert.True(item.CompletedAtUtc >= item.CreatedAtUtc);
        }

    }
}
