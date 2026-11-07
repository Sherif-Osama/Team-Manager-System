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
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Common")]
    public sealed class ScheduleProjectIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<HttpResponseMessage> ScheduleAsync(Guid projectId, DateOnly? startDate, DateOnly? dueDate,
            string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectSchedule(projectId))
            {
                Content = JsonContent.Create(new { startDate, dueDate })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task ScheduleProject_AsOwner_SchedulesProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-owner");
            var (_, projectId) = await _helpers.CreateProjectAsync("Schedule Me", ownerToken);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
            var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

            var response = await ScheduleAsync(projectId, startDate, dueDate, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);

            Assert.Equal(startDate, project.StartDate);
            Assert.Equal(dueDate, project.DueDate);
        }

        [Fact]
        public async Task ScheduleProject_AsOwner_WithNullDates_KeepsExistingSchedule()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-null");
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
            var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12));
            var (_, projectId) = await _helpers.CreateProjectAsync("Keep Schedule", ownerToken, startDate, dueDate);

            var response = await ScheduleAsync(projectId, null, null, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);

            Assert.Equal(startDate, project.StartDate);
            Assert.Equal(dueDate, project.DueDate);
        }

        [Fact]
        public async Task ScheduleProject_AsProjectMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-member");

            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);
            var create = await _helpers.CreateProjectRequestAsync(teamId, new { name = "Member Schedule", description = (string?)null }, ownerToken);
            var projectId = await create.Content.ReadFromJsonAsync<Guid>();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var project = await db.Projects.SingleAsync(p => p.Id == projectId);
                project.AddMember(memberId, ProjectRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await ScheduleAsync(projectId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ScheduleProject_AsNonMember_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-owner");
            var (_, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-outsider");
            var (_, projectId) = await _helpers.CreateProjectAsync("Outsider Schedule", ownerToken);

            var response = await ScheduleAsync(projectId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ScheduleProject_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-notoken");
            var (_, projectId) = await _helpers.CreateProjectAsync("No Token Schedule", ownerToken);

            var response = await ScheduleAsync(projectId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ScheduleProject_WithUnknownProjectId_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-unknown");

            var response = await ScheduleAsync(Guid.NewGuid(),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task ScheduleProject_WithConflictingTaskDates_Returns409AndKeepsSchedule()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("sched-conflict");

            var projectStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var projectDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
            var (_, projectId) = await _helpers.CreateProjectAsync("Conflict Schedule", ownerToken, projectStart, projectDue);

            var taskResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                ProjectEndpoints.CreateTask(projectId))
            {
                Content = JsonContent.Create(new
                {
                    title = "Late Task",
                    priority = TaskPriority.Medium,
                    startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                    dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15))
                }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            });

            Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);

            var newDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8));

            var response = await ScheduleAsync(projectId, null, newDue, ownerToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);

            Assert.Equal(projectStart, project.StartDate);
            Assert.Equal(projectDue, project.DueDate);
        }
    }
}
