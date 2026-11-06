using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Outbox;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Authentication.Configuration;

namespace TeamManager.IntegrationTest.Authentication
{
    [Collection("Authentication")]
    public sealed class RegisterIntegrationTests(AuthenticationFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly string _registerEndpoint = AuthenticationEndpoints.Register;
        private sealed record RegisterResponse(Guid Id);

        [Fact]
        public async Task Register_ValidRequest_ShouldCreateUserAndOutboxMessage()
        {
            var email = $"integration-{Guid.NewGuid():N}@example.com";

            var request = new
            {
                email,
                displayName = "Integration Test User",
                password = "P@ssword1234"
            };

            var response = await _client.PostAsJsonAsync(_registerEndpoint, request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Email == email);

            Assert.Equal(request.displayName, user.DisplayName);

            Assert.NotEqual(request.password, user.PasswordHash);

            Assert.False(user.IsEmailConfirmed);

            Assert.NotNull(user.EmailConfirmationTokenHash);

            Assert.True(user.EmailConfirmationTokenExpiresAtUtc > DateTime.UtcNow);

            var defaultRole = await db.Roles.AsNoTracking().SingleAsync(r => r.Name == "User");
            var userRole = await db.UserRoles.AsNoTracking().SingleAsync(ur => ur.UserId == user.Id);

            Assert.Equal(defaultRole.Id, userRole.RoleId);

            // Assert - Outbox
            var outboxMessage = await db.OutboxMessages.SingleAsync(x => x.Payload.Contains(email));

            Assert.Equal("EmailConfirmationEmail", outboxMessage.Type);
        }

        [Fact]
        public async Task Register_WithPendingInvitations_LinksAllInvitationsToNewUser()
        {
            var ownerEmail = $"inv-owner-{Guid.NewGuid():N}@example.com";

            var ownerResponse = await _client.PostAsJsonAsync(_registerEndpoint, new
            {
                email = ownerEmail,
                displayName = "Invitation Owner",
                password = "P@ssword1234"
            });

            Assert.Equal(HttpStatusCode.Created, ownerResponse.StatusCode);
            var owner = await ownerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

            var targetEmail = $"invitee-{Guid.NewGuid():N}@example.com";

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

                var team1 = new Team(Guid.NewGuid(), $"InvTeam1-{Guid.NewGuid():N}", owner!.Id, owner.Id);
                var invitation1 = team1.Invite(targetEmail, null, owner.Id, TeamRole.Member, "hash-1", DateTime.UtcNow.AddDays(1));

                // second team invites the same email with different casing to prove case-insensitive linking
                var team2 = new Team(Guid.NewGuid(), $"InvTeam2-{Guid.NewGuid():N}", owner.Id, owner.Id);
                var invitation2 = team2.Invite(targetEmail.ToUpperInvariant(), null, owner.Id, TeamRole.Admin, "hash-2", DateTime.UtcNow.AddDays(2));

                db.Teams.AddRange(team1, team2);
                db.TeamInvitations.AddRange(invitation1, invitation2);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email = targetEmail, displayName = "Invitee", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

                var invitations = await db.TeamInvitations.AsNoTracking()
                    .Where(i => i.InvitedEmail == targetEmail)
                    .ToListAsync();

                Assert.Equal(2, invitations.Count);
                Assert.All(invitations, invitation =>
                {
                    Assert.Equal(result!.Id, invitation.InvitedUserId);
                    Assert.Equal(TeamInvitationStatus.Pending, invitation.Status);
                });
            }
        }

        [Fact]
        public async Task Register_AfterSoftDelete_AllowsSameEmailAgain()
        {
            var email = $"reuse-{Guid.NewGuid():N}@example.com";

            var firstResponse = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "First Registration", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
            var firstUser = await firstResponse.Content.ReadFromJsonAsync<RegisterResponse>();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == firstUser!.Id);
                user.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var secondResponse = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Re-Registered", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var activeUsers = await db2.Users.Where(u => u.Email == email && u.DeletedAtUtc == null).ToListAsync();

            Assert.Single(activeUsers);
        }

        [Fact]
        public async Task Register_EmailComparison_IsCaseInsensitive()
        {
            var email = $"Case-{Guid.NewGuid():N}@Example.com";

            var firstResponse = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Original Casing", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var secondResponse = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email = email.ToLowerInvariant(), displayName = "Lower Casing", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.Users.CountAsync(u => u.Email == email));
        }


        [Fact]
        public async Task Register_WhenDefaultRoleMissing_Returns404WithoutPersistingUser()
        {
            var email = $"no-role-{Guid.NewGuid():N}@example.com";
            var disabledRoleName = $"User-disabled-{Guid.NewGuid():N}";

            try
            {
                using (var scope = factory.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                    var role = await db.Roles.SingleAsync(r => r.Name == "User");
                    role.Rename(disabledRoleName);
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                var response = await _client.PostAsJsonAsync(_registerEndpoint,
                    new { email, displayName = "No Role User", password = "P@ssword1234" });

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
            finally
            {
                using var scope = factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var role = await db.Roles.SingleAsync(r => r.Name == disabledRoleName);
                role.Rename("User");
                await db.SaveChangesAsync(CancellationToken.None);
            }

            using var verifyScope = factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.False(await verifyDb.Users.AnyAsync(u => u.Email == email));
            Assert.False(await verifyDb.OutboxMessages.AnyAsync(o => o.Payload.Contains(email)));
        }

        [Fact]
        public async Task Register_WhenOutboxWriteFails_Returns500AndRollsBackUser()
        {
            var client = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOutbox>();
                services.AddScoped<IOutbox>(_ => new ThrowingOutbox());
            })).CreateClient();

            var email = $"rollback-{Guid.NewGuid():N}@example.com";

            var response = await client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Rollback User", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.False(await db.Users.AnyAsync(u => u.Email == email));
            Assert.False(await db.OutboxMessages.AnyAsync(o => o.Payload.Contains(email)));
        }

        [Fact]
        public async Task Register_ConcurrentRegistrationsWithDifferentEmails_BothSucceed()
        {
            var emails = new List<string> { $"parallel-{Guid.NewGuid():N}@example.com",
                $"parallel-{Guid.NewGuid():N}@example.com" };

            var responses = await Task.WhenAll(emails.Select(email =>
                _client.PostAsJsonAsync(_registerEndpoint,
                    new { email, displayName = "Parallel User", password = "P@ssword1234" })));

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            foreach (var email in emails)
                Assert.True(await db.Users.AnyAsync(u => u.Email == email));
        }

        [Fact]
        public async Task Register_ConcurrentDuplicateRegistrations_ExactlyOneSucceeds()
        {
            var email = $"dupe-{Guid.NewGuid():N}@example.com";

            var emails = new List<string> { email, email };

            var responses = await Task.WhenAll(emails.Select(email =>
                _client.PostAsJsonAsync(_registerEndpoint,
                    new { email, displayName = "Duplicate Racer", password = "P@ssword1234" })));

            var statuses = responses.Select(r => r.StatusCode).OrderBy(code => code).ToArray();

            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.Users.CountAsync(u => u.Email == email));
        }

        private sealed class ThrowingOutbox : IOutbox
        {
            public Task Add(OutboxMessageType type, string payload) =>
                throw new InvalidOperationException("Forced outbox failure in integration test.");
        }
    }
}
