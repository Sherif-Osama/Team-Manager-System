using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Task;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Tasks")]
    public sealed class GetMyTasksIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid TeamId, Guid ProjectId, Guid OwnerId, string OwnerToken)> CreateOwnedProjectAsync(string prefix)
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner", $"{prefix} Owner");
            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Tasks {Guid.NewGuid():N}", ownerToken);

            return (teamId, projectId, ownerId, ownerToken);
        }

        private async Task<(Guid UserId, string Token)> AddMemberAsync(Guid teamId, Guid projectId, string label, string ownerToken)
        {
            var (userId, _, token) = await _authenticatedUser.CreateConfirmedUserAsync(label.Replace(' ', '-'), label);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, userId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            await _helpers.AddProjectMemberAsync(projectId, userId, ProjectRole.Member, ownerToken);

            return (userId, token);
        }

        private async Task<long> CreateAssignedTaskAsync(Guid projectId, string title, Guid assigneeId, string ownerToken,
            TaskPriority priority = TaskPriority.Medium, string? description = null, DateOnly? dueDate = null)
        {
            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title, description, assigneeUserId = assigneeId, priority, dueDate }, ownerToken);

            return await created.Content.ReadFromJsonAsync<long>();
        }

        [Fact]
        public async Task GetMyTasks_ListsOnlyTheCallersAssignedTasks()
        {
            var (teamId, projectId, ownerId, ownerToken) = await CreateOwnedProjectAsync("mytasks");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "My Assignee", ownerToken);
            var (otherId, _) = await AddMemberAsync(teamId, projectId, "Other Assignee", ownerToken);

            var mine = await CreateAssignedTaskAsync(projectId, "Mine", memberId, ownerToken);
            await CreateAssignedTaskAsync(projectId, "Others", otherId, ownerToken);
            await _helpers.CreateTaskAsync(projectId, "Unassigned", ownerToken);

            var result = await _helpers.GetMyTasksResultAsync(memberToken);

            Assert.Equal(1, result.TotalCount);

            var task = Assert.Single(result.Items);

            Assert.Equal(mine, task.Id);
            Assert.Equal(projectId, task.ProjectId);
            Assert.Equal(memberId, task.AssigneeUserId);
            Assert.Equal("My Assignee", task.AssigneeUserName);
            Assert.Equal("Mine", task.Title);
            Assert.Equal(ownerId, task.CreatedBy);
            Assert.Equal("mytasks Owner", task.CreatorName);
            Assert.Equal(TaskItemStatus.Todo, task.Status);
            Assert.Equal(TaskPriority.Medium, task.Priority);
            Assert.Empty(task.Labels);
        }

        [Fact]
        public async Task GetMyTasks_WithoutAccessToken_Returns401()
        {
            var response = await _helpers.GetMyTasksAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMyTasks_SearchMatchesTitleDescriptionOrProjectName()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("mytasks-search");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "Search Assignee", ownerToken);

            await CreateAssignedTaskAsync(projectId, "Findable task", memberId, ownerToken);
            await CreateAssignedTaskAsync(projectId, "Hidden task", memberId, ownerToken, description: "Needle buried in the description");

            var otherProjectId = (await _helpers.CreateProjectAsync($"Zeta Board {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;
            await _helpers.AddProjectMemberAsync(otherProjectId, memberId, ProjectRole.Member, ownerToken);
            await CreateAssignedTaskAsync(otherProjectId, "In the zeta project", memberId, ownerToken);

            var byTitle = await _helpers.GetMyTasksResultAsync(memberToken, "search=findable");
            var byDescription = await _helpers.GetMyTasksResultAsync(memberToken, "search=needle");
            var byProjectName = await _helpers.GetMyTasksResultAsync(memberToken, "search=zeta");
            var trimmed = await _helpers.GetMyTasksResultAsync(memberToken, "search=%20needle%20");

            Assert.Equal(1, byTitle.TotalCount);
            Assert.Equal("Findable task", Assert.Single(byTitle.Items).Title);
            Assert.Equal(1, byDescription.TotalCount);
            Assert.Equal("Hidden task", Assert.Single(byDescription.Items).Title);
            Assert.Equal(1, byProjectName.TotalCount);
            Assert.Equal("In the zeta project", Assert.Single(byProjectName.Items).Title);
            Assert.Equal(1, trimmed.TotalCount);
        }

        [Fact]
        public async Task GetMyTasks_FiltersByStatusAndPriority()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("mytasks-filters");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "Filter Assignee", ownerToken);

            await CreateAssignedTaskAsync(projectId, "Todo high", memberId, ownerToken, priority: TaskPriority.High);
            await CreateAssignedTaskAsync(projectId, "Todo low", memberId, ownerToken, priority: TaskPriority.Low);
            var progressHigh = await CreateAssignedTaskAsync(projectId, "Progress high", memberId, ownerToken, priority: TaskPriority.High);

            var toInProgress = await _helpers.ChangeTaskStatusAsync(progressHigh, TaskItemStatus.InProgress, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, toInProgress.StatusCode);

            var todo = await _helpers.GetMyTasksResultAsync(memberToken, "status=Todo");
            var high = await _helpers.GetMyTasksResultAsync(memberToken, "priority=High");
            var combined = await _helpers.GetMyTasksResultAsync(memberToken, "status=InProgress&priority=High");

            Assert.Equal(2, todo.TotalCount);
            Assert.Equal(2, high.TotalCount);
            Assert.Equal(progressHigh, Assert.Single(combined.Items).Id);
        }

        [Fact]
        public async Task GetMyTasks_IgnoresTheProjectIdFilter()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("mytasks-project");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "Project Assignee", ownerToken);

            await CreateAssignedTaskAsync(projectId, "First project task", memberId, ownerToken);

            var otherProjectId = (await _helpers.CreateProjectAsync($"Tasks {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;
            await _helpers.AddProjectMemberAsync(otherProjectId, memberId, ProjectRole.Member, ownerToken);
            await CreateAssignedTaskAsync(otherProjectId, "Other project task", memberId, ownerToken);

            var result = await _helpers.GetMyTasksResultAsync(memberToken, $"projectId={projectId}");

            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetMyTasks_ExcludesDeletedTasksAndTasksInDeletedProjects()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("mytasks-deleted");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "Deleted Assignee", ownerToken);

            var doomed = await CreateAssignedTaskAsync(projectId, "Doomed task", memberId, ownerToken);
            await _helpers.DeleteTaskAsync(doomed, ownerToken);

            var otherProjectId = (await _helpers.CreateProjectAsync($"Tasks {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;
            await _helpers.AddProjectMemberAsync(otherProjectId, memberId, ProjectRole.Member, ownerToken);
            var orphan = await CreateAssignedTaskAsync(otherProjectId, "Orphan task", memberId, ownerToken);

            var cancel = await _helpers.ChangeTaskStatusAsync(orphan, TaskItemStatus.Cancelled, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(otherProjectId), ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var alive = await CreateAssignedTaskAsync(projectId, "Alive task", memberId, ownerToken);

            var result = await _helpers.GetMyTasksResultAsync(memberToken);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(alive, Assert.Single(result.Items).Id);
        }



        [Fact]
        public async Task GetMyTasks_WithEmptyProjectId_ReturnsAllTasks()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("mytasks-emptyproject");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "Empty Project Assignee", ownerToken);

            await CreateAssignedTaskAsync(projectId, "Still listed task", memberId, ownerToken);

            var emptyGuid = await _helpers.GetMyTasksResultAsync(memberToken, "projectId=00000000-0000-0000-0000-000000000000");
            var emptyString = await _helpers.GetMyTasksResultAsync(memberToken, "projectId=");

            Assert.Equal(1, emptyGuid.TotalCount);
            Assert.Equal(1, emptyString.TotalCount);
        }


        [Fact]
        public async Task GetMyTasks_PagesTasksWithDueDatesBeforeUndatedOnes()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("mytasks-paging");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "Paging Assignee", ownerToken);

            var datedOne = await CreateAssignedTaskAsync(projectId, "Dated one", memberId, ownerToken,
                dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
            var datedTwo = await CreateAssignedTaskAsync(projectId, "Dated two", memberId, ownerToken,
                dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
            var undatedOne = await CreateAssignedTaskAsync(projectId, "Undated one", memberId, ownerToken);
            var undatedTwo = await CreateAssignedTaskAsync(projectId, "Undated two", memberId, ownerToken);

            var pageOne = await _helpers.GetMyTasksResultAsync(memberToken, "pageSize=2");
            var pageTwo = await _helpers.GetMyTasksResultAsync(memberToken, "pageSize=2&page=2");

            Assert.Equal(4, pageOne.TotalCount);
            Assert.Equal(new[] { datedOne, datedTwo }, pageOne.Items.Select(t => t.Id).OrderBy(id => id));
            Assert.Equal(new[] { undatedOne, undatedTwo }, pageTwo.Items.Select(t => t.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task GetMyTasks_BeyondTheLastPageReturnsNoRows()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("mytasks-beyondpage");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, "Beyond Assignee", ownerToken);

            await CreateAssignedTaskAsync(projectId, "Solo task", memberId, ownerToken);

            var result = await _helpers.GetMyTasksResultAsync(memberToken, "page=5");

            Assert.Equal(1, result.TotalCount);
            Assert.Empty(result.Items);
        }
    }
}
