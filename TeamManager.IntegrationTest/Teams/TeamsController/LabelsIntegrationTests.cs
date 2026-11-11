using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Team;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Teams")]
    public sealed class LabelsIntegrationTests(TeamFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record CreateLabelResponse(long LabelId);

        private sealed record GetLabelsItemResponseDto(long LabelId, string Name, string ColorHex, DateTime CreatedAtUtc);

        private sealed record GetLabelsResponseDto(List<GetLabelsItemResponseDto> Items, int Page, int PageSize, int TotalCount);

        private async Task<HttpResponseMessage> CreateLabelAsync(Guid teamId, string name, string? colorHex, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Labels(teamId))
            {
                Content = JsonContent.Create(new { name, colorHex })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> UpdateLabelAsync(Guid teamId, long labelId, string name, string? colorHex, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, TeamEndpoints.Label(teamId, labelId))
            {
                Content = JsonContent.Create(new { name, colorHex })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> DeleteLabelAsync(Guid teamId, long labelId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, TeamEndpoints.Label(teamId, labelId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> GetLabelsAsync(Guid teamId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.Labels(teamId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task CreateLabel_AsOwner_CreatesLabelAndInvalidatesLabelCache()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-owner");
            var teamId = await _helpers.CreateTeamAsync("Label Team One", null, ownerToken);

            var response = await CreateLabelAsync(teamId, "  Urgent  ", "#FF0000", ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreateLabelResponse>();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var label = await db.Labels.AsNoTracking().SingleAsync(l => l.Id == result!.LabelId);

            Assert.Equal("Urgent", label.Name);
            Assert.Equal("#FF0000", label.ColorHex);
            Assert.Equal(teamId, label.TeamId);

            var list = await GetLabelsAsync(teamId, ownerToken);
            var listResult = await list.Content.ReadFromJsonAsync<GetLabelsResponseDto>();

            Assert.Contains(listResult!.Items, i => i.LabelId == result?.LabelId);
        }

        [Fact]
        public async Task CreateLabel_AsAdmin_CreatesLabel()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-admin");

            var teamId = await _helpers.CreateTeamAsync("Label Team Two", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(adminId, TeamRole.Admin);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await CreateLabelAsync(teamId, "Admin Label", "#00FF00", adminToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateLabel_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-member");

            var teamId = await _helpers.CreateTeamAsync("Label Team Three", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await CreateLabelAsync(teamId, "Forbidden Label", null, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateLabel_WithDuplicateName_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-dup");
            var teamId = await _helpers.CreateTeamAsync("Label Team Four", null, ownerToken);

            var first = await CreateLabelAsync(teamId, "Duplicate Label", null, ownerToken);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await CreateLabelAsync(teamId, "duplicate label", null, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }


        [Fact]
        public async Task UpdateLabel_AsOwner_UpdatesNameAndColor()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-update");
            var teamId = await _helpers.CreateTeamAsync("Label Team Six", null, ownerToken);

            var created = await CreateLabelAsync(teamId, "Old Label", "#000000", ownerToken);
            var createdResult = await created.Content.ReadFromJsonAsync<CreateLabelResponse>();

            var response = await UpdateLabelAsync(teamId, createdResult!.LabelId, "  New Label  ", "#ABCDEF", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var label = await db.Labels.AsNoTracking().SingleAsync(l => l.Id == createdResult.LabelId);

            Assert.Equal("New Label", label.Name);
            Assert.Equal("#ABCDEF", label.ColorHex);
        }

        [Fact]
        public async Task UpdateLabel_WithUnknownLabelId_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-update-unknown");
            var teamId = await _helpers.CreateTeamAsync("Label Team Seven", null, ownerToken);

            var response = await UpdateLabelAsync(teamId, 999999, "Ghost", null, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteLabel_AsOwner_DeletesLabel()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-delete");
            var teamId = await _helpers.CreateTeamAsync("Label Team Eight", null, ownerToken);

            var created = await CreateLabelAsync(teamId, "Disposable", null, ownerToken);
            var createdResult = await created.Content.ReadFromJsonAsync<CreateLabelResponse>();

            var response = await DeleteLabelAsync(teamId, createdResult!.LabelId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.False(await db.Labels.AnyAsync(l => l.Id == createdResult.LabelId));
        }

        [Fact]
        public async Task DeleteLabel_WithUnknownLabelId_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-delete-unknown");
            var teamId = await _helpers.CreateTeamAsync("Label Team Nine", null, ownerToken);

            var response = await DeleteLabelAsync(teamId, 999999, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetLabels_AsMember_ReturnsLabels()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("label-viewer");

            var teamId = await _helpers.CreateTeamAsync("Label Team Ten", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                team.AddLabel("Shared Label", "#123456");
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await GetLabelsAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GetLabelsResponseDto>();

            Assert.Contains(result!.Items, i => i.Name == "Shared Label");
        }
    }
}
