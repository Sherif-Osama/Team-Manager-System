using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Projects")]
    public sealed class DeleteProjectIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<HttpResponseMessage> DeleteAsync(Guid projectId, string? accessToken = null)
            => await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), accessToken);

        private async Task<DateTime?> GetDeletedAtUtcAsync(Guid projectId)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            return await db.Projects.AsNoTracking().Where(p => p.Id == projectId).Select(p => p.DeletedAtUtc).SingleAsync();
        }

        [Fact]
        public async Task DeleteProject_AsOwner_Returns204AndSoftDeletesProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-owner");
            var (_, projectId) = await _helpers.CreateProjectAsync("Delete Me", ownerToken);

            var before = DateTime.UtcNow;
            var response = await DeleteAsync(projectId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var deletedAtUtc = await GetDeletedAtUtcAsync(projectId);

            Assert.NotNull(deletedAtUtc);
            Assert.InRange(deletedAtUtc!.Value, before.AddDays(-1), DateTime.UtcNow);
        }

        [Fact]
        public async Task DeleteProject_AsOwner_HidesProjectFromRetrieval()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-hidden");
            var (_, projectId) = await _helpers.CreateProjectAsync("Hidden After Delete", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(projectId, ownerToken)).StatusCode);

            var getResponse = await _helpers.RequestAsync(HttpMethod.Get, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteProject_Twice_Returns404OnSecondCall()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-twice");
            var (_, projectId) = await _helpers.CreateProjectAsync("Delete Twice", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(projectId, ownerToken)).StatusCode);

            var second = await DeleteAsync(projectId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
            Assert.NotNull(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_WithActiveTask_Returns409AndKeepsProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-active");
            var (_, projectId) = await _helpers.CreateProjectAsync("Blocked By Task", ownerToken);

            await _helpers.CreateTaskAsync(projectId, "Open Task", ownerToken);

            var response = await DeleteAsync(projectId, ownerToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Null(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_WithCompletedTask_Returns409AndKeepsProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-done");
            var (_, projectId) = await _helpers.CreateProjectAsync("Blocked By Done Task", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Finished Task", ownerToken);

            foreach (var status in new[] { TaskItemStatus.InProgress, TaskItemStatus.InReview, TaskItemStatus.Done })
                Assert.Equal(HttpStatusCode.NoContent, (await _helpers.ChangeTaskStatusAsync(taskId, status, ownerToken)).StatusCode);

            var response = await DeleteAsync(projectId, ownerToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Null(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_WithCancelledAndDeletedTasks_Returns204()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-inactive");
            var (_, projectId) = await _helpers.CreateProjectAsync("Only Inactive Tasks", ownerToken);

            var cancelledTaskId = await _helpers.CreateTaskAsync(projectId, "Cancelled Task", ownerToken);
            var deletedTaskId = await _helpers.CreateTaskAsync(projectId, "Deleted Task", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeTaskStatusAsync(cancelledTaskId, TaskItemStatus.Cancelled, ownerToken)).StatusCode);

            await _helpers.DeleteTaskAsync(deletedTaskId, ownerToken);

            var response = await DeleteAsync(projectId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_AsProjectMember_Returns403AndKeepsProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-member");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-member-user");
            var (_, projectId) = await _helpers.CreateProjectAsync("Member Delete", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var project = await db.Projects.SingleAsync(p => p.Id == projectId);
                project.AddMember(memberId, ProjectRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await DeleteAsync(projectId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Null(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_AsTeamOwnerWithoutProjectMembership_Returns204()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-teamowner-promoted");

            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            var create = await _helpers.CreateProjectRequestAsync(teamId,
                new { name = "Team Owner Delete", description = (string?)null }, ownerToken);
            var projectId = await create.Content.ReadFromJsonAsync<Guid>();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(teamOwnerId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var response = await DeleteAsync(projectId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_AsOutsider_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-outsider");
            var (_, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-outsider-user");
            var (_, projectId) = await _helpers.CreateProjectAsync("Outsider Delete", ownerToken);

            var response = await DeleteAsync(projectId, outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Null(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-notoken");
            var (_, projectId) = await _helpers.CreateProjectAsync("No Token Delete", ownerToken);

            var response = await DeleteAsync(projectId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(await GetDeletedAtUtcAsync(projectId));
        }

        [Fact]
        public async Task DeleteProject_WithUnknownProjectId_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-unknown");

            var response = await DeleteAsync(Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProject_WithEmptyProjectId_Returns400WithFieldError()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-empty");

            var response = await DeleteAsync(Guid.Empty, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("ProjectId", out _));
        }

        [Fact]
        public async Task DeleteProject_FreesProjectNameForRecreationInSameTeam()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-rename");
            var (teamId, projectId) = await _helpers.CreateProjectAsync("Reused Name", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(projectId, ownerToken)).StatusCode);

            var recreate = await _helpers.CreateProjectRequestAsync(teamId,
                new { name = "Reused Name", description = (string?)null }, ownerToken);

            Assert.Equal(HttpStatusCode.Created, recreate.StatusCode);

            var newProjectId = await recreate.Content.ReadFromJsonAsync<Guid>();
            Assert.NotEqual(projectId, newProjectId);
            Assert.Null(await GetDeletedAtUtcAsync(newProjectId));
        }

        [Fact]
        public async Task DeleteProject_ConcurrentRequests_SucceedOnce()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("delete-racing");
            var (_, projectId) = await _helpers.CreateProjectAsync("Racing Delete", ownerToken);

            var responses = await Task.WhenAll(DeleteAsync(projectId, ownerToken), DeleteAsync(projectId, ownerToken));

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.NoContent));
            Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.NoContent
                or HttpStatusCode.NotFound or HttpStatusCode.Conflict, $"Unexpected status {(int)r.StatusCode}"));

            Assert.NotNull(await GetDeletedAtUtcAsync(projectId));
        }
    }
}
