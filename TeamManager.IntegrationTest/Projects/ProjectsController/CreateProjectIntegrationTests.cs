using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Projects")]
    public sealed class CreateProjectIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private static object ProjectBody(string name, string? description = null,
            DateOnly? startDate = null, DateOnly? dueDate = null) => new { name, description, startDate, dueDate };

        [Fact]
        public async Task CreateProject_AsOwner_CreatesProjectWithOwnerMembership()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-owner");
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            var response = await _helpers.CreateProjectRequestAsync(teamId, ProjectBody("My Project", "A description"), ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var projectId = await response.Content.ReadFromJsonAsync<Guid>();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);

            Assert.Equal("My Project", project.Name);
            Assert.Equal("A description", project.Description);
            Assert.Equal(teamId, project.TeamId);
            Assert.Equal(ownerId, project.OwnerUserId);
            Assert.Equal(ProjectStatus.Active, project.Status);
            Assert.Null(project.DeletedAtUtc);

            var ownerMembership = await db.ProjectMembers.AsNoTracking().SingleAsync(m => m.ProjectId == projectId);

            Assert.Equal(ownerId, ownerMembership.UserId);
            Assert.Equal(ProjectRole.Owner, ownerMembership.ProjectRole);
        }

        [Fact]
        public async Task CreateProject_AsAdmin_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-admin");

            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(adminId, TeamRole.Admin);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.CreateProjectRequestAsync(teamId, ProjectBody("Admin Project"), adminToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateProject_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-member");

            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.CreateProjectRequestAsync(teamId, ProjectBody("Member Project"), memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateProject_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-notoken");
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            var response = await _helpers.CreateProjectRequestAsync(teamId, ProjectBody("No Token Project"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateProject_WithDuplicateName_Returns409()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-dup");
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            var first = await _helpers.CreateProjectRequestAsync(teamId, ProjectBody("Duplicate Project"), ownerToken);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _helpers.CreateProjectRequestAsync(teamId, ProjectBody("duplicate project"), ownerToken);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.Projects.CountAsync(p => p.TeamId == teamId && p.Name == "Duplicate Project"));
        }

        [Fact]
        public async Task CreateProject_SameNameInDifferentTeams_IsAllowed()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-multiteam");

            var team1 = await _teamHelpers.CreateTeamAsync("Team 1", null, ownerToken);
            var team2 = await _teamHelpers.CreateTeamAsync("Team 2", null, ownerToken);

            var first = await _helpers.CreateProjectRequestAsync(team1, ProjectBody("Shared Name"), ownerToken);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _helpers.CreateProjectRequestAsync(team2, ProjectBody("Shared Name"), ownerToken);

            Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        }

        [Fact]
        public async Task CreateProject_WithDates_CreatesScheduledProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-dates");
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

            var response = await _helpers.CreateProjectRequestAsync(teamId,
                ProjectBody("Scheduled Project", null, startDate, dueDate), ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var projectId = await response.Content.ReadFromJsonAsync<Guid>();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);

            Assert.Equal(startDate, project.StartDate);
            Assert.Equal(dueDate, project.DueDate);
        }


        [Fact]
        public async Task CreateProject_ConcurrentDuplicates_ExactlyOneSucceeds()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("proj-race");
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
                _helpers.CreateProjectRequestAsync(teamId, ProjectBody("Racing Project"), ownerToken)));

            var statuses = responses.Select(r => r.StatusCode).OrderBy(code => code).ToArray();

            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.Projects.CountAsync(p => p.TeamId == teamId && p.Name == "Racing Project"));
        }
    }
}
