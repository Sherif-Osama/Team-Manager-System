using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserByEmail;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Admin.Configuration;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Admin
{
    [Collection("Common")]
    public sealed class AdminUsersQueryIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly AdminTestHelpers _helpers = new(factory);

        [Fact]
        public async Task GetUsers_AsAdmin_ReturnsPagedUsers()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            await _helpers.CreateRegularUserAsync("getusers-target1");

            var result = await _helpers.GetUsersResultAsync(adminToken, "page=1&pageSize=10");

            Assert.NotNull(result);
            Assert.True(result.TotalCount > 0);
            Assert.NotEmpty(result.Users);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task GetUsers_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync("getusers-reg");

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.Users, userToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_WithoutToken_Returns401()
        {
            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.Users);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_FiltersBySearch()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var uniquePrefix = $"searchmatch-{Guid.NewGuid():N}";
            await _helpers.CreateRegularUserAsync(uniquePrefix);

            var result = await _helpers.GetUsersResultAsync(adminToken, $"search={uniquePrefix}");

            Assert.True(result.TotalCount >= 1);
            Assert.All(result.Users, u => Assert.Contains(uniquePrefix, u.Email, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetUsers_FiltersByIsActive()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (inactiveUserId, _, _) = await _helpers.CreateRegularUserAsync("activefilter-inactive");

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == inactiveUserId);
                user.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var inactiveResults = await _helpers.GetUsersResultAsync(adminToken, "isActive=false");
            Assert.Contains(inactiveResults.Users, u => u.Id == inactiveUserId && !u.IsActive);

            var activeResults = await _helpers.GetUsersResultAsync(adminToken, "isActive=true");
            Assert.DoesNotContain(activeResults.Users, u => u.Id == inactiveUserId);
        }

        [Fact]
        public async Task GetUserById_AsAdmin_ReturnsUserDetailsAndRoles()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, email, _) = await _helpers.CreateRegularUserAsync("getuserbyid-target");

            var result = await _helpers.GetUserByIdResultAsync(targetUserId, adminToken);

            Assert.Equal(targetUserId, result.Id);
            Assert.Equal(email, result.Email);
            Assert.True(result.IsEmailConfirmed);
            Assert.True(result.IsActive);
            Assert.Contains(DefaultRoles.User, result.SystemRolesNames);
        }

        [Fact]
        public async Task GetUserById_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync("getuserbyid-reg");

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.UserById(Guid.NewGuid()), userToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserById_WhenUserNotFound_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.UserById(Guid.NewGuid()), adminToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUserByEmail_AsAdmin_ReturnsUserDetails()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, email, _) = await _helpers.CreateRegularUserAsync("getbyemail-target");

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.UserByEmail(email), adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<GetUserByEmailResponse>();
            Assert.NotNull(result);
            Assert.Equal(targetUserId, result.Id);
            Assert.Equal(email, result.Email);
            Assert.Contains(DefaultRoles.User, result.SystemRolesNames);
        }

        [Fact]
        public async Task GetUserByEmail_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync("getbyemail-reg");

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.UserByEmail("target@example.com"), userToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserByEmail_WhenEmailNotFound_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.UserByEmail("nonexistent-email@example.com"), adminToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
