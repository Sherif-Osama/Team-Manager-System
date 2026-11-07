using System.Net;
using TeamManager.IntegrationTest.Authentication.Configuration;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Common")]
    public sealed class ChangePasswordIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private const string NewPassword = "N@wPassword5678";

        [Fact]
        public async Task ChangePassword_WithValidCredentials_Returns204AndTheNewPasswordLogsIn()
        {
            var (_, email, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("password-valid");

            var response = await _helpers.ChangePasswordRequestAsync(
                new { currentPassword = "P@ssword1234", newPassword = NewPassword, confirmNewPassword = NewPassword },
                accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await _helpers.RequestAsync(HttpMethod.Post, AuthenticationEndpoints.Login,
                body: new { email, password = NewPassword })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _helpers.RequestAsync(HttpMethod.Post, AuthenticationEndpoints.Login,
                body: new { email, password = "P@ssword1234" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _helpers.GetMyProfileAsync(accessToken)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithWrongCurrentPassword_Returns401()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("password-wrongcurrent");

            var response = await _helpers.ChangePasswordRequestAsync(
                new { currentPassword = "WrongPassword123", newPassword = NewPassword, confirmNewPassword = NewPassword },
                accessToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithAMismatchedConfirmation_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("password-mismatch");

            var response = await _helpers.ChangePasswordRequestAsync(
                new { currentPassword = "P@ssword1234", newPassword = NewPassword, confirmNewPassword = "Different5678" },
                accessToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "ConfirmNewPassword");
        }

        [Fact]
        public async Task ChangePassword_WithANewPasswordShorterThan8Characters_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("password-weak");

            var response = await _helpers.ChangePasswordRequestAsync(
                new { currentPassword = "P@ssword1234", newPassword = "Short7!", confirmNewPassword = "Short7!" },
                accessToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "NewPassword");
        }

        [Fact]
        public async Task ChangePassword_WithTheCurrentPasswordAsTheNewOne_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("password-same");

            var response = await _helpers.ChangePasswordRequestAsync(
                new { currentPassword = "P@ssword1234", newPassword = "P@ssword1234", confirmNewPassword = "P@ssword1234" },
                accessToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "NewPassword");
        }

        [Fact]
        public async Task ChangePassword_WithoutAccessToken_Returns401()
        {
            var response = await _helpers.ChangePasswordRequestAsync(
                new { currentPassword = "P@ssword1234", newPassword = NewPassword, confirmNewPassword = NewPassword });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
