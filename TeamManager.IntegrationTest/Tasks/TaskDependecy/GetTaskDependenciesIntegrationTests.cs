using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskDependecy
{
    [Collection("Common")]
    public sealed class GetTaskDependenciesIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<long> AddPredecessorAsync(long dependentId, Guid projectId, string title, string ownerToken)
        {
            var predecessorId = await _helpers.CreateTaskAsync(projectId, title, ownerToken);

            await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            return predecessorId;
        }

        [Fact]
        public async Task GetTaskDependencies_AsProjectViewer_ReturnsPredecessorDetails()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Dependency Get Viewer", ownerToken);

            var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9));
            var predecessorId = await _helpers.CreateTaskAsync(projectId, "Dated predecessor", ownerToken, dueDate: dueDate);
            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);

            var dependencyId = await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            var listed = await _helpers.GetTaskDependenciesResultAsync(dependentId, viewerToken);

            Assert.Equal(1, listed.TotalCount);
            Assert.Equal(1, listed.Page);
            Assert.Equal(20, listed.PageSize);

            var item = Assert.Single(listed.Items);

            Assert.Equal(dependencyId, item.DependencyId);
            Assert.Equal(predecessorId, item.DependsOnTaskId);
            Assert.Equal("Dated predecessor", item.DependsOnTaskTitle);
            Assert.Equal(dueDate, item.DependsOnTaskDueDate);
            Assert.Equal(TaskItemStatus.Todo, item.DependsOnTaskStatus);
            Assert.Equal(TaskPriority.Medium, item.DependsOnTaskPriority);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, item.CreatedByName);
        }

        [Fact]
        public async Task GetTaskDependencies_ForTaskWithoutDependencies_ReturnsEmptyList()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get-empty");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Solo task", ownerToken);

            var listed = await _helpers.GetTaskDependenciesResultAsync(taskId, ownerToken);

            Assert.Empty(listed.Items);
            Assert.Equal(0, listed.TotalCount);
        }

        [Fact]
        public async Task GetTaskDependencies_DoesNotListTheReverseEdge()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get-direction");

            var predecessorId = await _helpers.CreateTaskAsync(projectId, "Predecessor", ownerToken);
            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);

            await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            var listed = await _helpers.GetTaskDependenciesResultAsync(predecessorId, ownerToken);

            Assert.Empty(listed.Items);
        }

        [Fact]
        public async Task GetTaskDependencies_PaginatesDependencies()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get-paging");

            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);

            var first = await AddPredecessorAsync(dependentId, projectId, "Predecessor one", ownerToken);
            var second = await AddPredecessorAsync(dependentId, projectId, "Predecessor two", ownerToken);
            var third = await AddPredecessorAsync(dependentId, projectId, "Predecessor three", ownerToken);

            var pageOne = await _helpers.GetTaskDependenciesResultAsync(dependentId, ownerToken, "page=1&pageSize=2");
            var pageTwo = await _helpers.GetTaskDependenciesResultAsync(dependentId, ownerToken, "page=2&pageSize=2");

            Assert.Equal(3, pageOne.TotalCount);
            Assert.Equal(2, pageOne.Items.Count);
            Assert.Equal(3, pageTwo.TotalCount);
            Assert.Single(pageTwo.Items);
            Assert.Equal(2, pageTwo.Page);
            Assert.Equal(2, pageTwo.PageSize);

            // The listing has no ordering column, so only the membership of both pages together is stable.
            var expected = new[] { first, second, third };

            Assert.Equal(expected, pageOne.Items.Concat(pageTwo.Items).Select(x => x.DependsOnTaskId).OrderBy(x => x).ToArray());
        }

        [Fact]
        public async Task GetTaskDependencies_AsTeamMemberWithoutProjectMembership_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get-teammember");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("dep-get-teammember-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var predecessorId = await _helpers.CreateTaskAsync(projectId, "Predecessor", ownerToken);
            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);

            await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            var response = await _helpers.GetTaskDependenciesAsync(dependentId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskDependencies_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Unlisted task", ownerToken);

            var response = await _helpers.GetTaskDependenciesAsync(taskId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskDependencies_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get-unknown");

            var response = await _helpers.GetTaskDependenciesAsync(long.MaxValue, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskDependencies_AfterTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-get-deleted");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.GetTaskDependenciesAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


    }
}
