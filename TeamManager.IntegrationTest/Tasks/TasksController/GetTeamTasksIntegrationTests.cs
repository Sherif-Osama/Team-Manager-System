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
    public sealed class GetTeamTasksIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid TeamId, string OwnerToken)> CreateTeamAsync(string prefix)
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner", $"{prefix} Owner");
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            return (teamId, ownerToken);
        }

        private async Task<Guid> CreateProjectAsync(Guid teamId, string ownerToken)
            => (await _helpers.CreateProjectAsync($"Tasks {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;

        private async Task<(Guid UserId, string Token)> AddTeamMemberAsync(Guid teamId, TeamRole role, string label,
            string ownerToken)
        {
            var (userId, _, token) = await _authenticatedUser.CreateConfirmedUserAsync(label.Replace(' ', '-'), label);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, userId, role, ownerToken)).IsSuccessStatusCode);

            return (userId, token);
        }

        [Fact]
        public async Task GetTeamTasks_AsTeamOwner_ListsAllTeamTasksAcrossProjects()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks");

            var firstProjectId = await CreateProjectAsync(teamId, ownerToken);
            var secondProjectId = await CreateProjectAsync(teamId, ownerToken);

            var first = await _helpers.CreateTaskAsync(firstProjectId, "First task", ownerToken);
            var second = await _helpers.CreateTaskAsync(secondProjectId, "Second task", ownerToken);

            var result = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(new[] { first, second }, result.Items.Select(t => t.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task GetTeamTasks_AsTeamMember_SeesOnlyTasksOfTheirProjects()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-member");

            var firstProjectId = await CreateProjectAsync(teamId, ownerToken);
            var secondProjectId = await CreateProjectAsync(teamId, ownerToken);

            var visible = await _helpers.CreateTaskAsync(firstProjectId, "Visible task", ownerToken);
            await _helpers.CreateTaskAsync(secondProjectId, "Hidden task", ownerToken);

            var (memberId, memberToken) = await AddTeamMemberAsync(teamId, TeamRole.Member, "Team Tasks Member", ownerToken);
            await _helpers.AddProjectMemberAsync(firstProjectId, memberId, ProjectRole.Member, ownerToken);

            var result = await _helpers.GetTeamTasksResultAsync(teamId, memberToken);

            Assert.Equal(1, result.TotalCount);

            var task = Assert.Single(result.Items);

            Assert.Equal(visible, task.Id);
            Assert.Equal(firstProjectId, task.ProjectId);
            Assert.Null(task.AssigneeUserId);
            Assert.Null(task.AssigneeUserName);
        }

        [Fact]
        public async Task GetTeamTasks_AsTeamMemberWithoutProjectMemberships_ReturnsEmptyList()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-empty");

            var projectId = await CreateProjectAsync(teamId, ownerToken);
            await _helpers.CreateTaskAsync(projectId, "Unseen task", ownerToken);

            var (_, memberToken) = await AddTeamMemberAsync(teamId, TeamRole.Member, "Team Tasks Empty", ownerToken);

            var result = await _helpers.GetTeamTasksResultAsync(teamId, memberToken);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetTeamTasks_AsTeamViewer_Returns403()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-viewer");

            var projectId = await CreateProjectAsync(teamId, ownerToken);
            await _helpers.CreateTaskAsync(projectId, "Viewer task", ownerToken);

            var (_, viewerToken) = await AddTeamMemberAsync(teamId, TeamRole.Viewer, "Team Tasks Viewer", ownerToken);

            var response = await _helpers.GetTeamTasksAsync(teamId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamTasks_AsUserOutsideTheTeam_Returns403()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-outsider");

            var projectId = await CreateProjectAsync(teamId, ownerToken);
            await _helpers.CreateTaskAsync(projectId, "Private task", ownerToken);

            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("teamtasks-outsider-stranger");

            var response = await _helpers.GetTeamTasksAsync(teamId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamTasks_WithoutAccessToken_Returns401()
        {
            var (teamId, _) = await CreateTeamAsync("teamtasks-notoken");

            var response = await _helpers.GetTeamTasksAsync(teamId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamTasks_ForUnknownTeam_Returns403()
        {
            var (_, ownerToken) = await CreateTeamAsync("teamtasks-unknown");

            var response = await _helpers.GetTeamTasksAsync(Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamTasks_AfterTeamIsDeleted_Returns403()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-deletedteam");

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, TeamEndpoints.Team(teamId), ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.GetTeamTasksAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task GetTeamTasks_FiltersByProject()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-project");

            var firstProjectId = await CreateProjectAsync(teamId, ownerToken);
            var secondProjectId = await CreateProjectAsync(teamId, ownerToken);

            var first = await _helpers.CreateTaskAsync(firstProjectId, "First project task", ownerToken);
            await _helpers.CreateTaskAsync(secondProjectId, "Second project task", ownerToken);

            var result = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, $"projectId={firstProjectId}");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(first, Assert.Single(result.Items).Id);
        }

        [Fact]
        public async Task GetTeamTasks_FiltersByStatusAndPriority()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-filters");

            var projectId = await CreateProjectAsync(teamId, ownerToken);

            var todoHigh = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Todo high", priority = TaskPriority.High }, ownerToken);
            var todoLow = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Todo low", priority = TaskPriority.Low }, ownerToken);
            var progressHighResponse = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Progress high", priority = TaskPriority.High }, ownerToken);

            Assert.Equal(HttpStatusCode.Created, todoHigh.StatusCode);
            Assert.Equal(HttpStatusCode.Created, todoLow.StatusCode);
            Assert.Equal(HttpStatusCode.Created, progressHighResponse.StatusCode);

            var progressHigh = await progressHighResponse.Content.ReadFromJsonAsync<long>();

            var toInProgress = await _helpers.ChangeTaskStatusAsync(progressHigh, TaskItemStatus.InProgress, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, toInProgress.StatusCode);

            var todo = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, "status=Todo");
            var high = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, "priority=High");
            var combined = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, "status=InProgress&priority=High");

            Assert.Equal(2, todo.TotalCount);
            Assert.Equal(2, high.TotalCount);
            Assert.Equal(progressHigh, Assert.Single(combined.Items).Id);
        }

        [Fact]
        public async Task GetTeamTasks_SearchMatchesTitleDescriptionOrProjectName()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-search");

            var projectId = await CreateProjectAsync(teamId, ownerToken);
            var needleProjectId = (await _helpers.CreateProjectAsync($"Needle Board {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;

            await _helpers.CreateTaskAsync(projectId, "Findable task", ownerToken);

            var hidden = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Hidden task", description = "Needle buried in the description", priority = TaskPriority.Medium },
                ownerToken);
            Assert.Equal(HttpStatusCode.Created, hidden.StatusCode);

            await _helpers.CreateTaskAsync(needleProjectId, "In the needle project", ownerToken);

            var byTitle = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, "search=findable");
            var byDescriptionOrProject = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, "search=needle");

            Assert.Equal(1, byTitle.TotalCount);
            Assert.Equal("Findable task", Assert.Single(byTitle.Items).Title);
            Assert.Equal(2, byDescriptionOrProject.TotalCount);
            Assert.Contains(byDescriptionOrProject.Items, t => t.Title == "Hidden task");
            Assert.Contains(byDescriptionOrProject.Items, t => t.Title == "In the needle project");
        }




        [Fact]
        public async Task GetTeamTasks_PagesTasksWithDueDatesBeforeUndatedOnes()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-paging");

            var projectId = await CreateProjectAsync(teamId, ownerToken);

            var datedOne = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Dated one", priority = TaskPriority.Medium, dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) },
                ownerToken);
            var datedTwo = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Dated two", priority = TaskPriority.Medium, dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ownerToken);
            Assert.Equal(HttpStatusCode.Created, datedOne.StatusCode);
            Assert.Equal(HttpStatusCode.Created, datedTwo.StatusCode);

            var undatedOne = await _helpers.CreateTaskAsync(projectId, "Undated one", ownerToken);
            var undatedTwo = await _helpers.CreateTaskAsync(projectId, "Undated two", ownerToken);

            var pageOne = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, "pageSize=2");
            var pageTwo = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken, "pageSize=2&page=2");

            var datedIds = new[] { await datedOne.Content.ReadFromJsonAsync<long>(), await datedTwo.Content.ReadFromJsonAsync<long>() };
            var undatedIds = new[] { undatedOne, undatedTwo };

            Assert.Equal(4, pageOne.TotalCount);
            Assert.Equal(datedIds.OrderBy(id => id).ToArray(), pageOne.Items.Select(t => t.Id).OrderBy(id => id));
            Assert.Equal(undatedIds.OrderBy(id => id).ToArray(), pageTwo.Items.Select(t => t.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task GetTeamTasks_ExcludesDeletedTasksAndTasksOfDeletedProjects()
        {
            var (teamId, ownerToken) = await CreateTeamAsync("teamtasks-deleted");

            var projectId = await CreateProjectAsync(teamId, ownerToken);
            var secondProjectId = await CreateProjectAsync(teamId, ownerToken);

            var doomed = await _helpers.CreateTaskAsync(projectId, "Doomed task", ownerToken);
            await _helpers.DeleteTaskAsync(doomed, ownerToken);

            var orphan = await _helpers.CreateTaskAsync(secondProjectId, "Orphan task", ownerToken);

            var cancel = await _helpers.ChangeTaskStatusAsync(orphan, TaskItemStatus.Cancelled, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(secondProjectId), ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var alive = await _helpers.CreateTaskAsync(projectId, "Alive task", ownerToken);

            var result = await _helpers.GetTeamTasksResultAsync(teamId, ownerToken);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(alive, Assert.Single(result.Items).Id);
        }
    }
}
