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
    public sealed class TransferOwnershipIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task TransferOwnership_AsOwner_TransfersToActiveMemberWithImmediateEffect()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-member");

            var (_, projectId) = await _helpers.CreateProjectAsync("Transfer Me", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var projectEntity = await db.Projects.SingleAsync(p => p.Id == projectId);
                projectEntity.AddMember(memberId, ProjectRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = memberId }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db2.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);

            Assert.Equal(memberId, project.OwnerUserId);

            var oldOwnerMembership = await db2.ProjectMembers.AsNoTracking()
                .SingleAsync(m => m.ProjectId == projectId && m.UserId == ownerId);
            var newOwnerMembership = await db2.ProjectMembers.AsNoTracking()
                .SingleAsync(m => m.ProjectId == projectId && m.UserId == memberId);

            Assert.Equal(ProjectRole.Admin, oldOwnerMembership.ProjectRole);
            Assert.Equal(ProjectRole.Owner, newOwnerMembership.ProjectRole);

            var updateAsNewOwner = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.Project(projectId))
            {
                Content = JsonContent.Create(new { name = "Updated By New Owner", description = (string?)null }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", memberToken) }
            });

            Assert.Equal(HttpStatusCode.NoContent, updateAsNewOwner.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_AsProjectMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-member");

            var (_, projectId) = await _helpers.CreateProjectAsync("Member Transfer", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var projectEntity = await db.Projects.SingleAsync(p => p.Id == projectId);
                projectEntity.AddMember(memberId, ProjectRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = memberId }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", memberToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_AsNonMember_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (outsiderId, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-outsider");
            var (_, projectId) = await _helpers.CreateProjectAsync("Outsider Transfer", ownerToken);

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = outsiderId }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-notoken");
            var (_, projectId) = await _helpers.CreateProjectAsync("No Token Transfer", ownerToken);

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = Guid.NewGuid() })
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_WithUnknownProjectId_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-unknown");

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(Guid.NewGuid()))
            {
                Content = JsonContent.Create(new { newOwnerUserId = Guid.NewGuid() }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToUnknownUser_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-target");
            var (_, projectId) = await _helpers.CreateProjectAsync("Unknown Target", ownerToken);

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = Guid.NewGuid() }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToInactiveUser_Returns404()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-inactive");

            var (_, projectId) = await _helpers.CreateProjectAsync("Inactive Target", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var projectEntity = await db.Projects.SingleAsync(p => p.Id == projectId);
                projectEntity.AddMember(memberId, ProjectRole.Member);
                var target = await db.Users.SingleAsync(u => u.Id == memberId);
                target.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = memberId }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToNonMember_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (outsiderId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-outsider");

            var (_, projectId) = await _helpers.CreateProjectAsync("NonMember Target", ownerToken);

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = outsiderId }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToSelf_Returns204WithoutChanges()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-self");
            var (_, projectId) = await _helpers.CreateProjectAsync("Self Transfer", ownerToken);

            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.ProjectOwner(projectId))
            {
                Content = JsonContent.Create(new { newOwnerUserId = ownerId }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);
            var membership = await db.ProjectMembers.AsNoTracking()
                .SingleAsync(m => m.ProjectId == projectId && m.UserId == ownerId);

            Assert.Equal(ownerId, project.OwnerUserId);
            Assert.Equal(ProjectRole.Owner, membership.ProjectRole);
        }

    }
}
