using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.Configuration
{
    public sealed class ProjectTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _teamHelpers = new(factory);

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

        // Membership ids are only exposed by the members listing endpoint, so the owner's row is read from
        // the database until that endpoint has its own coverage.
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

        public async Task<long> CreateTaskAsync(Guid projectId, string title, string accessToken)
        {
            var response = await RequestAsync(HttpMethod.Post, ProjectEndpoints.CreateTask(projectId), accessToken,
                new { title, priority = TaskPriority.Medium });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<long>();
        }

        public async Task<HttpResponseMessage> ChangeTaskStatusAsync(long taskId, TaskItemStatus status, string accessToken)
            => await RequestAsync(HttpMethod.Put, ProjectEndpoints.TaskStatus(taskId), accessToken, new { status });

        public async Task DeleteTaskAsync(long taskId, string accessToken)
        {
            var response = await RequestAsync(HttpMethod.Delete, ProjectEndpoints.Task(taskId), accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
