using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Application.Features.Permissions.Queries.GetPermissions;
using TeamManager.Application.Features.Roles.Queries.GetRoleById;
using TeamManager.Application.Features.Roles.Queries.GetRoles;
using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById;
using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUsers;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Admin.Configuration
{
    public sealed class AdminTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private record CreateRoleResponse(int RoleId);

        public async Task<(Guid UserId, string Email, string Token)> CreateAdminUserAsync()
        {
            var (userId, email) = await _authenticatedUser.RegisterAndConfirmUserAsync();
            await _authenticatedUser.PromoteToAdminAsync(userId);
            var login = await _authenticatedUser.LoginAsync(email);
            return (userId, email, login.AccessToken);
        }

        public async Task<(Guid UserId, string Email, string Token)> CreateRegularUserAsync(string prefix = "user")
        {
            var (userId, email, token) = await _authenticatedUser.CreateConfirmedUserAsync(prefix);
            return (userId, email, token);
        }

        public async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string url, string? accessToken = null, object? body = null)
        {
            var request = new HttpRequestMessage(method, url)
            {

                Content = JsonContent.Create(body),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            };
            return await _client.SendAsync(request);
        }

        public async Task<int> CreateRoleAsync(string name, string? description, string adminToken)
        {
            var response = await RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, adminToken, new { Name = name, Description = description });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<CreateRoleResponse>())?.RoleId ?? 0;
        }

        public async Task<GetRoleByIdResponse> GetRoleByIdResultAsync(int roleId, string adminToken)
        {
            var response = await RequestAsync(HttpMethod.Get, AdminEndpoints.Role(roleId), adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<GetRoleByIdResponse>())!;
        }

        public async Task<GetRolesResponse> GetRolesResultAsync(string adminToken)
        {
            var response = await RequestAsync(HttpMethod.Get, AdminEndpoints.Roles, adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<GetRolesResponse>())!;
        }

        public async Task<GetUsersResponse> GetUsersResultAsync(string adminToken, string? queryParams = null)
        {
            var url = string.IsNullOrWhiteSpace(queryParams) ? AdminEndpoints.Users : $"{AdminEndpoints.Users}?{queryParams}";
            var response = await RequestAsync(HttpMethod.Get, url, adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<GetUsersResponse>())!;
        }

        public async Task<GetUserByIdResponse> GetUserByIdResultAsync(Guid userId, string adminToken)
        {
            var response = await RequestAsync(HttpMethod.Get, AdminEndpoints.UserById(userId), adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<GetUserByIdResponse>())!;
        }

        public async Task<GetPermissionsResponse> GetPermissionsResultAsync(string adminToken)
        {
            var response = await RequestAsync(HttpMethod.Get, AdminEndpoints.Permissions, adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<GetPermissionsResponse>())!;
        }
    }
}
