using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskChecklist
{
    [Collection("Common")]
    public sealed class AddChecklistItemIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task AddChecklistItem_AsProjectOwner_ReturnsTheItemIdAndStoresIt()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Owner task", ownerToken);

            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Write the docs", ownerToken);

            Assert.True(itemId > 0);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken);

            var item = Assert.Single(items);

            Assert.Equal(itemId, item.Id);
            Assert.Equal(taskId, item.TaskId);
            Assert.Equal("Write the docs", item.Content);
            Assert.False(item.IsCompleted);
            Assert.Equal((short)0, item.SortOrder);
            Assert.Null(item.CompletedAtUtc);
            Assert.Null(item.CompletedBy);
            Assert.Null(item.CompletedByName);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(1, task.ChecklistItemsCount);
        }

        [Fact]
        public async Task AddChecklistItem_TouchesTheTask()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-touch");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Touched task", ownerToken);

            Assert.Null((await _helpers.GetTaskResultAsync(taskId, ownerToken)).UpdatedAtUtc);

            await _helpers.AddChecklistItemAsync(taskId, "Touches the parent", ownerToken);

            Assert.NotNull((await _helpers.GetTaskResultAsync(taskId, ownerToken)).UpdatedAtUtc);
        }

        [Fact]
        public async Task AddChecklistItem_OnADoneTask_Returns200AndLeavesTheTaskDoneWithAnOpenItem()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-done");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Done task", ownerToken);

            await _helpers.CompleteTaskAsync(taskId, ownerToken);

            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Opened after completion", ownerToken);

            var item = Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));

            Assert.Equal(itemId, item.Id);
            Assert.False(item.IsCompleted);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(TaskItemStatus.Done, task.Status);
            Assert.Equal(1, task.ChecklistItemsCount);
        }

        [Fact]
        public async Task AddChecklistItem_AsProjectAdminOrMember_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-roles");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Checklist Admin", ownerToken);
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Checklist Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Roles task", ownerToken);

            var asAdmin = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "Admin item" }, adminToken);
            var asMember = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "Member item" }, memberToken);

            Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
            Assert.Equal(HttpStatusCode.OK, asMember.StatusCode);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken);

            Assert.Equal(new[] { "Admin item", "Member item" }, items.Select(x => x.Content).ToArray());
        }

        [Fact]
        public async Task AddChecklistItem_AsProjectViewer_Returns403AndStoresNothing()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-viewer");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Checklist Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Viewer task", ownerToken);

            var response = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "Viewer item" }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Empty(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));
        }

        [Fact]
        public async Task AddChecklistItem_AsTeamMemberWithoutProjectMembership_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-teammember");
            var (teamMemberId, _, teamMemberToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-add-teammember-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamMemberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team member task", ownerToken);

            var response = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "Team member item" }, teamMemberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Empty(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));
        }

        [Fact]
        public async Task AddChecklistItem_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-add-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Outsider task", ownerToken);

            var response = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "Outsider item" }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddChecklistItem_AsTeamOwnerWithoutProjectMembership_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-add-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner task", ownerToken);

            var response = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "Team owner item" }, teamOwnerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddChecklistItem_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token task", ownerToken);

            var response = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "No token item" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddChecklistItem_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-unknowntask");

            var response = await _helpers.AddChecklistItemRequestAsync(long.MaxValue, new { content = "Item" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddChecklistItem_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed task", ownerToken);

            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.AddChecklistItemRequestAsync(taskId, new { content = "Item" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }




        [Fact]
        public async Task AddChecklistItem_WithThreeHundredCharacters_Returns200()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-add-maxcontent");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Boundary content task", ownerToken);

            var content = new string('a', 300);

            var itemId = await _helpers.AddChecklistItemAsync(taskId, content, ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken);

            Assert.Equal(content, Assert.Single(items).Content);
            Assert.True(itemId > 0);
        }
    }
}
