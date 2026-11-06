using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskChecklist
{
    [Collection("Projects")]
    public sealed class RemoveChecklistItemIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task RemoveChecklistItem_AsProjectOwner_Returns204AndDeletesTheRow()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Remove task", ownerToken);

            await _helpers.AddChecklistItemAsync(taskId, "Keep one", ownerToken);
            var removableItemId = await _helpers.AddChecklistItemAsync(taskId, "Remove me", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Keep two", ownerToken);

            var response = await _helpers.RemoveChecklistItemRequestAsync(taskId, removableItemId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken);

            Assert.Equal(new[] { "Keep one", "Keep two" }, items.Select(x => x.Content).ToArray());
            Assert.DoesNotContain(removableItemId, items.Select(x => x.Id).ToArray());

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(2, task.ChecklistItemsCount);
        }

        [Fact]
        public async Task RemoveChecklistItem_AsProjectAdminOrMember_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-roles");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Checklist Admin", ownerToken);
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Checklist Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Roles remove task", ownerToken);
            var adminItemId = await _helpers.AddChecklistItemAsync(taskId, "Admin removes", ownerToken);
            var memberItemId = await _helpers.AddChecklistItemAsync(taskId, "Member removes", ownerToken);

            var asAdmin = await _helpers.RemoveChecklistItemRequestAsync(taskId, adminItemId, adminToken);
            var asMember = await _helpers.RemoveChecklistItemRequestAsync(taskId, memberItemId, memberToken);

            Assert.Equal(HttpStatusCode.NoContent, asAdmin.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, asMember.StatusCode);

            Assert.Empty(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));
        }

        [Fact]
        public async Task RemoveChecklistItem_AsProjectViewer_Returns403AndKeepsTheItem()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-viewer");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Checklist Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Viewer remove task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Viewer cannot remove", ownerToken);

            var response = await _helpers.RemoveChecklistItemRequestAsync(taskId, itemId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));
        }

        [Fact]
        public async Task RemoveChecklistItem_AsTeamOwnerWithoutProjectMembership_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-remove-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner remove task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Team owner removes", ownerToken);

            var response = await _helpers.RemoveChecklistItemRequestAsync(taskId, itemId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RemoveChecklistItem_ForAnItemOfAnotherTask_Returns400AndKeepsTheOtherItem()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-foreign");

            var otherTaskId = await _helpers.CreateTaskAsync(projectId, "Foreign item owner", ownerToken);
            var foreignItemId = await _helpers.AddChecklistItemAsync(otherTaskId, "Belongs to another task", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Remove foreign task", ownerToken);

            var response = await _helpers.RemoveChecklistItemRequestAsync(taskId, foreignItemId, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "Checklist item not found");

            Assert.Single(await _helpers.GetTaskChecklistResultAsync(otherTaskId, ownerToken));
        }

        [Fact]
        public async Task RemoveChecklistItem_KeepsTheSortOrderGapsAndANewItemTakesTheNextOrder()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-sortorder");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Sort order task", ownerToken);

            var firstItemId = await _helpers.AddChecklistItemAsync(taskId, "First", ownerToken);
            var secondItemId = await _helpers.AddChecklistItemAsync(taskId, "Second", ownerToken);
            await _helpers.AddChecklistItemAsync(taskId, "Third", ownerToken);

            await _helpers.RemoveChecklistItemAsync(taskId, secondItemId, ownerToken);

            var fourthItemId = await _helpers.AddChecklistItemAsync(taskId, "Fourth", ownerToken);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken);

            Assert.Equal(new[] { "First", "Third", "Fourth" }, items.Select(x => x.Content).ToArray());
            Assert.Equal(new short[] { 0, 2, 3 }, items.Select(x => x.SortOrder).ToArray());
            Assert.Equal(firstItemId, items[0].Id);
            Assert.Equal(fourthItemId, items[^1].Id);
        }

        [Fact]
        public async Task RemoveChecklistItem_ForUnknownTask_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-unknowntask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Unknown task remove", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Orphan remove target", ownerToken);

            var response = await _helpers.RemoveChecklistItemRequestAsync(long.MaxValue, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveChecklistItem_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed remove task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Deleted task item", ownerToken);

            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.RemoveChecklistItemRequestAsync(taskId, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveChecklistItem_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-remove-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token remove task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "No token remove", ownerToken);

            var response = await _helpers.RemoveChecklistItemRequestAsync(taskId, itemId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


    }
}
