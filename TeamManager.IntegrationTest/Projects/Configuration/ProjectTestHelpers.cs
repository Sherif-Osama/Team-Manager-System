using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Tasks.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.Configuration
{
    public sealed class ProjectTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        public async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string url, string? accessToken = null, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            if (body is not null)
                request.Content = JsonContent.Create(body);

            return await _client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> CreateProjectRequestAsync(Guid teamId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Post, ProjectEndpoints.CreateProject(teamId), accessToken, body);

        public async Task<(Guid TeamId, Guid ProjectId)> CreateProjectAsync(string name, string ownerToken,
            DateOnly? startDate = null, DateOnly? dueDate = null, string? description = null, Guid? teamId = null)
        {
            var resolvedTeamId = teamId ?? await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);
            var response = await CreateProjectRequestAsync(resolvedTeamId, new { name, description, startDate, dueDate }, ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (resolvedTeamId, await response.Content.ReadFromJsonAsync<Guid>());
        }

        public async Task<long> AddProjectMemberAsync(Guid projectId, Guid userId, ProjectRole role, string accessToken)
        {
            var response = await AddProjectMemberRequestAsync(projectId, new { userId, projectRole = role }, accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<long>();
        }

        public async Task<HttpResponseMessage> AddProjectMemberRequestAsync(Guid projectId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Post, ProjectEndpoints.Members(projectId), accessToken, body);

        public async Task<HttpResponseMessage> RemoveProjectMemberRequestAsync(Guid projectId, long memberId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Delete, ProjectEndpoints.Member(projectId, memberId), accessToken);

        public async Task RemoveProjectMemberAsync(Guid projectId, long memberId, string accessToken)
        {
            var response = await RemoveProjectMemberRequestAsync(projectId, memberId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> ChangeMemberRoleRequestAsync(Guid projectId, long memberId, object role,
            string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.MemberRole(projectId, memberId), accessToken, role);

        public async Task ChangeMemberRoleAsync(Guid projectId, long memberId, ProjectRole role, string accessToken)
        {
            var response = await ChangeMemberRoleRequestAsync(projectId, memberId, role, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> GetProjectMembersAsync(Guid projectId, string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.Members(projectId) : $"{ProjectEndpoints.Members(projectId)}?{query}", accessToken);

        public async Task<GetProjectMembersResponseDto> GetProjectMembersResultAsync(Guid projectId, string accessToken,
            string? query = null)
        {
            var response = await GetProjectMembersAsync(projectId, accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetProjectMembersResponseDto>())!;
        }

        // The members listing does not return the membership row id, so a mutation target is still read
        // from the database.
        public async Task<long> GetProjectMemberIdAsync(Guid projectId, Guid userId)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var member = await db.ProjectMembers.AsNoTracking().SingleAsync(m => m.ProjectId == projectId &&
                m.UserId == userId && m.Status == ProjectMemberStatus.Active);

            return member.Id;
        }

        public async Task<HttpResponseMessage> GetProjectAsync(Guid projectId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Get, ProjectEndpoints.Project(projectId), accessToken);

        public async Task<ProjectResponseDto> GetProjectResultAsync(Guid projectId, string accessToken)
        {
            var response = await GetProjectAsync(projectId, accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<ProjectResponseDto>())!;
        }

        public async Task<HttpResponseMessage> GetProjectByNameAsync(string name, string? accessToken = null)
            => await RequestAsync(HttpMethod.Get,
                $"{ProjectEndpoints.ProjectByName}?name={Uri.EscapeDataString(name)}", accessToken);

        public async Task<HttpResponseMessage> ChangeProjectStatusAsync(Guid projectId, ProjectStatus status, string accessToken)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.ProjectStatus(projectId), accessToken, new { status });

        public async Task<HttpResponseMessage> CreateTaskRequestAsync(Guid projectId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Post, ProjectEndpoints.CreateTask(projectId), accessToken, body);

        public async Task<long> CreateTaskAsync(Guid projectId, string title, string accessToken,
            DateOnly? startDate = null, DateOnly? dueDate = null)
        {
            var response = await CreateTaskRequestAsync(projectId,
                new { title, priority = TaskPriority.Medium, startDate, dueDate }, accessToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<long>();
        }

        public async Task<HttpResponseMessage> GetTaskAsync(long taskId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Get, ProjectEndpoints.Task(taskId), accessToken);

        public async Task<GetTaskResponseDto> GetTaskResultAsync(long taskId, string accessToken)
        {
            var response = await GetTaskAsync(taskId, accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetTaskResponseDto>())!;
        }

        public async Task<HttpResponseMessage> UpdateTaskRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.Task(taskId), accessToken, body);

        public async Task<HttpResponseMessage> ChangeTaskStatusRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskStatus(taskId), accessToken, body);

        public async Task<HttpResponseMessage> ChangeTaskStatusAsync(long taskId, TaskItemStatus status, string accessToken)
            => await ChangeTaskStatusRequestAsync(taskId, new { status }, accessToken);

        // The workflow only reaches InReview through InProgress, and only an Owner or Admin may go beyond it.
        public async Task MoveTaskThroughWorkflowAsync(long taskId, string accessToken)
        {
            foreach (var status in new[] { TaskItemStatus.InProgress, TaskItemStatus.InReview })
                Assert.Equal(HttpStatusCode.NoContent, (await ChangeTaskStatusAsync(taskId, status, accessToken)).StatusCode);
        }

        public async Task CompleteTaskAsync(long taskId, string accessToken)
        {
            await MoveTaskThroughWorkflowAsync(taskId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, (await ChangeTaskStatusAsync(taskId, TaskItemStatus.Done, accessToken)).StatusCode);
        }

        public async Task<HttpResponseMessage> RescheduleTaskRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskSchedule(taskId), accessToken, body);

        public async Task<HttpResponseMessage> ChangeTaskPriorityRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskPriority(taskId), accessToken, body);

        public async Task<HttpResponseMessage> AssignTaskRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskAssignee(taskId), accessToken, body);

        public async Task AssignTaskAsync(long taskId, Guid userId, string accessToken)
        {
            var response = await AssignTaskRequestAsync(taskId, new { userId }, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> UnassignTaskRequestAsync(long taskId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Delete, ProjectEndpoints.TaskAssignee(taskId), accessToken);

        public async Task UnassignTaskAsync(long taskId, string accessToken)
        {
            var response = await UnassignTaskRequestAsync(taskId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> DeleteTaskRequestAsync(long taskId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Delete, ProjectEndpoints.Task(taskId), accessToken);

        public async Task DeleteTaskAsync(long taskId, string accessToken)
        {
            var response = await DeleteTaskRequestAsync(taskId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> AddTaskDependencyRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Post, ProjectEndpoints.TaskDependencies(taskId), accessToken, body);

        public async Task<long> AddTaskDependencyAsync(long taskId, long dependsOnTaskId, string accessToken)
        {
            var response = await AddTaskDependencyRequestAsync(taskId, new { dependsOnTaskId }, accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<long>();
        }

        public async Task<HttpResponseMessage> DeleteTaskDependencyRequestAsync(long taskId, long dependencyId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Delete, ProjectEndpoints.TaskDependency(taskId, dependencyId), accessToken);

        public async Task DeleteTaskDependencyAsync(long taskId, long dependencyId, string accessToken)
        {
            var response = await DeleteTaskDependencyRequestAsync(taskId, dependencyId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> GetTaskDependenciesAsync(long taskId, string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.TaskDependencies(taskId) : $"{ProjectEndpoints.TaskDependencies(taskId)}?{query}",
                accessToken);

        public async Task<GetTaskDependenciesResponseDto> GetTaskDependenciesResultAsync(long taskId, string accessToken,
            string? query = null)
        {
            var response = await GetTaskDependenciesAsync(taskId, accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetTaskDependenciesResponseDto>())!;
        }

        public async Task<HttpResponseMessage> AddChecklistItemRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Post, ProjectEndpoints.TaskChecklist(taskId), accessToken, body);

        public async Task<long> AddChecklistItemAsync(long taskId, string content, string accessToken)
        {
            var response = await AddChecklistItemRequestAsync(taskId, new { content }, accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<long>();
        }

        public async Task<HttpResponseMessage> CompleteChecklistItemRequestAsync(long taskId, long itemId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskChecklistItemComplete(taskId, itemId), accessToken);

        public async Task CompleteChecklistItemAsync(long taskId, long itemId, string accessToken)
        {
            var response = await CompleteChecklistItemRequestAsync(taskId, itemId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> ReopenChecklistItemRequestAsync(long taskId, long itemId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskChecklistItemReopen(taskId, itemId), accessToken);

        public async Task ReopenChecklistItemAsync(long taskId, long itemId, string accessToken)
        {
            var response = await ReopenChecklistItemRequestAsync(taskId, itemId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> RemoveChecklistItemRequestAsync(long taskId, long itemId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Delete, ProjectEndpoints.TaskChecklistItem(taskId, itemId), accessToken);

        public async Task RemoveChecklistItemAsync(long taskId, long itemId, string accessToken)
        {
            var response = await RemoveChecklistItemRequestAsync(taskId, itemId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> GetTaskChecklistAsync(long taskId, string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.TaskChecklist(taskId) : $"{ProjectEndpoints.TaskChecklist(taskId)}?{query}",
                accessToken);

        public async Task<IReadOnlyList<TaskChecklistItemDto>> GetTaskChecklistResultAsync(long taskId, string accessToken,
            string? query = null)
        {
            var response = await GetTaskChecklistAsync(taskId, accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<IReadOnlyList<TaskChecklistItemDto>>())!;
        }

        public async Task<HttpResponseMessage> AddTaskCommentRequestAsync(long taskId, object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Post, ProjectEndpoints.TaskComments(taskId), accessToken, body);

        public async Task<long> AddTaskCommentAsync(long taskId, string content, string accessToken,
            IReadOnlyCollection<Guid>? mentionedUserIds = null)
        {
            var response = await AddTaskCommentRequestAsync(taskId, new { content, mentionedUserIds }, accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<long>();
        }

        public async Task<HttpResponseMessage> EditTaskCommentRequestAsync(long taskId, long commentId, object body,
            string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskComment(taskId, commentId), accessToken, body);

        public async Task EditTaskCommentAsync(long taskId, long commentId, string content, string accessToken,
            IReadOnlyCollection<Guid>? mentionedUserIds = null)
        {
            var response = await EditTaskCommentRequestAsync(taskId, commentId,
                new { content, mentionedUserIds }, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> DeleteTaskCommentRequestAsync(long taskId, long commentId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Delete, ProjectEndpoints.TaskComment(taskId, commentId), accessToken);

        public async Task DeleteTaskCommentAsync(long taskId, long commentId, string accessToken)
        {
            var response = await DeleteTaskCommentRequestAsync(taskId, commentId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> GetTaskCommentsAsync(long taskId, string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.TaskComments(taskId) : $"{ProjectEndpoints.TaskComments(taskId)}?{query}",
                accessToken);

        public async Task<GetTaskCommentsResponseDto> GetTaskCommentsResultAsync(long taskId, string accessToken,
            string? query = null)
        {
            var response = await GetTaskCommentsAsync(taskId, accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetTaskCommentsResponseDto>())!;
        }

        public async Task<HttpResponseMessage> GetMyCommentsAsync(string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.MyComments : $"{ProjectEndpoints.MyComments}?{query}", accessToken);

        public async Task<GetMyCommentsResponseDto> GetMyCommentsResultAsync(string accessToken, string? query = null)
        {
            var response = await GetMyCommentsAsync(accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetMyCommentsResponseDto>())!;
        }

        public async Task<HttpResponseMessage> GetMyMentionsAsync(string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.MyMentions : $"{ProjectEndpoints.MyMentions}?{query}", accessToken);

        public async Task<GetMyMentionsResponseDto> GetMyMentionsResultAsync(string accessToken, string? query = null)
        {
            var response = await GetMyMentionsAsync(accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetMyMentionsResponseDto>())!;
        }

        public async Task<HttpResponseMessage> GetMyTasksAsync(string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.MyTasks : $"{ProjectEndpoints.MyTasks}?{query}", accessToken);

        public async Task<GetMyTasksResponseDto> GetMyTasksResultAsync(string accessToken, string? query = null)
        {
            var response = await GetMyTasksAsync(accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetMyTasksResponseDto>())!;
        }

        public async Task<HttpResponseMessage> GetTeamTasksAsync(Guid teamId, string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? ProjectEndpoints.TeamTasks(teamId) : $"{ProjectEndpoints.TeamTasks(teamId)}?{query}", accessToken);

        public async Task<GetTeamTasksResponseDto> GetTeamTasksResultAsync(Guid teamId, string accessToken, string? query = null)
        {
            var response = await GetTeamTasksAsync(teamId, accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetTeamTasksResponseDto>())!;
        }

        public async Task<(Guid TeamId, Guid ProjectId, string OwnerToken)> CreateOwnedProjectAsync(string prefix)
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner");
            var (teamId, projectId) = await CreateProjectAsync($"Project {Guid.NewGuid():N}", ownerToken);

            return (teamId, projectId, ownerToken);
        }

        public async Task<(Guid UserId, string Token)> AddProjectMemberAsNewUserAsync(Guid teamId, Guid projectId,
            ProjectRole role, string displayName, string ownerToken)
        {
            var (userId, _, token) = await _authenticatedUser.CreateConfirmedUserAsync(displayName.Replace(' ', '-'), displayName);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, userId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            await AddProjectMemberAsync(projectId, userId, role, ownerToken);

            return (userId, token);
        }

        public async Task<(Guid ProjectId, Guid UserId, string OwnerToken)> CreateProjectWithTeamMemberAsync(string prefix)
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner");
            var (userId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-target");

            var (teamId, projectId) = await CreateProjectAsync($"Roster {Guid.NewGuid():N}", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, userId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            return (projectId, userId, ownerToken);
        }
    }
}
