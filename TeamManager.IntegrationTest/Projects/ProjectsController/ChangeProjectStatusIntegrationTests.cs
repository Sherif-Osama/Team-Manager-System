using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Common")]
    public sealed class ChangeProjectStatusIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<HttpResponseMessage> ChangeStatusAsync(Guid projectId, ProjectStatus status,
            string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectStatus(projectId))
            {
                Content = JsonContent.Create(new { status })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        private async Task<ProjectStatus> GetStatusAsync(Guid projectId)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            return await db.Projects.AsNoTracking().Where(p => p.Id == projectId).Select(p => p.Status).SingleAsync();
        }

        [Fact]
        public async Task ChangeProjectStatus_AsOwner_ActiveToCompleted()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-owner");

            (_, Guid projectId) = await _helpers.CreateProjectAsync("Complete Me", ownerToken);

            var response = await ChangeStatusAsync(projectId, ProjectStatus.Completed, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(ProjectStatus.Completed, await GetStatusAsync(projectId));
        }

        [Fact]
        public async Task ChangeProjectStatus_AsOwner_WalksThroughTransitionCycle()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-cycle");
            var (_, projectId) = await _helpers.CreateProjectAsync("Cycle Me", ownerToken);

            var steps = new[]
            {
                ProjectStatus.OnHold,
                ProjectStatus.Completed,
                ProjectStatus.Archived,
                ProjectStatus.Active
            };

            foreach (var target in steps)
            {
                var response = await ChangeStatusAsync(projectId, target, ownerToken);

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.Equal(target, await GetStatusAsync(projectId));
            }
        }

        [Fact]
        public async Task ChangeProjectStatus_AsProjectMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-member");


            (Guid teamId, Guid projectId) = await _helpers.CreateProjectAsync("Member Status", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var project = await db.Projects.SingleAsync(p => p.Id == projectId);
                project.AddMember(memberId, ProjectRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await ChangeStatusAsync(projectId, ProjectStatus.Completed, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeProjectStatus_AsNonMember_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-owner");
            var (_, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-outsider");
            (_, Guid projectId) = await _helpers.CreateProjectAsync("Outsider Status", ownerToken);

            var response = await ChangeStatusAsync(projectId, ProjectStatus.Completed, outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeProjectStatus_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-notoken");
            (_, Guid projectId) = await _helpers.CreateProjectAsync("No Token Status", ownerToken);

            var response = await ChangeStatusAsync(projectId, ProjectStatus.Completed);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeProjectStatus_WithUnknownProjectId_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-unknown");

            var response = await ChangeStatusAsync(Guid.NewGuid(), ProjectStatus.Completed, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeProjectStatus_ToSameStatus_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-same");
            (_, Guid projectId) = await _helpers.CreateProjectAsync("Same Status", ownerToken);

            var response = await ChangeStatusAsync(projectId, ProjectStatus.Active, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task ChangeProjectStatus_CompletedProject_RejectsNewTasks()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-tasks");
            (_, Guid projectId) = await _helpers.CreateProjectAsync("Completed Project", ownerToken);

            var complete = await ChangeStatusAsync(projectId, ProjectStatus.Completed, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

            var taskResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, ProjectEndpoints.CreateTask(projectId))
            {
                Content = JsonContent.Create(new { title = "Late Task", priority = TaskPriority.Medium }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            });

            Assert.Equal(HttpStatusCode.BadRequest, taskResponse.StatusCode);
        }
    }
}
