using System.Net;
using TeamManager.IntegrationTest.Authentication.Configuration;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Users")]
    public sealed class ConfirmEmailIntegrationTests(UsersFixture factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task ConfirmEmail_WithTheTokenFromTheConfirmationEmail_Returns204AndSwitchesTheEmail()
        {
            var (_, oldEmail, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("confirm-valid");
            var newEmail = $"confirm-valid-{Guid.NewGuid():N}@example.com";

            await _helpers.ChangeEmailRequestAsync(new { currentPassword = "P@ssword1234", newEmail }, accessToken);

            var token = await _helpers.GetLatestEmailConfirmationTokenAsync(newEmail);

            var response = await _helpers.ConfirmEmailRequestAsync(new { token }, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var profile = await _helpers.GetMyProfileResultAsync(accessToken);

            Assert.Equal(newEmail, profile.Email);
            Assert.True(profile.IsEmailConfirmed);

            await _authenticatedUser.LoginAsync(newEmail);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _helpers.RequestAsync(HttpMethod.Post,
                AuthenticationEndpoints.Login, body: new { email = oldEmail, password = "P@ssword1234" })).StatusCode);
        }

        [Fact]
        public async Task ConfirmEmail_WithAWrongToken_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("confirm-wrongtoken");
            var newEmail = $"confirm-wrong-{Guid.NewGuid():N}@example.com";

            await _helpers.ChangeEmailRequestAsync(new { currentPassword = "P@ssword1234", newEmail }, accessToken);

            var response = await _helpers.ConfirmEmailRequestAsync(new { token = "not-the-right-token" }, accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "Invalid confirmation token");
        }

        [Fact]
        public async Task ConfirmEmail_WithoutAPendingConfirmation_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("confirm-nopending");

            var response = await _helpers.ConfirmEmailRequestAsync(new { token = "any-token" }, accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "no pending email confirmation");
        }

        [Fact]
        public async Task ConfirmEmail_WithAnEmptyToken_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("confirm-emptytoken");

            var response = await _helpers.ConfirmEmailRequestAsync(new { token = "" }, accessToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "Token");
        }

        [Fact]
        public async Task ConfirmEmail_WithoutAccessToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await _helpers.ConfirmEmailRequestAsync(new { token = "any-token" })).StatusCode);
        }

        [Fact]
        public async Task ResendEmailConfirmation_ForAnUnconfirmedUser_Returns204AndEmitsANewConfirmationEmail()
        {
            var (_, email) = await _authenticatedUser.RegisterUserAsync("resend-unconfirmed");
            var accessToken = (await _authenticatedUser.LoginAsync(email)).AccessToken;

            Assert.Equal(1, await _helpers.CountEmailConfirmationMessagesAsync(email));

            var response = await _helpers.ResendEmailConfirmationAsync(accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(2, await _helpers.CountEmailConfirmationMessagesAsync(email));

            var token = await _helpers.GetLatestEmailConfirmationTokenAsync(email);
            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ConfirmEmailRequestAsync(new { token }, accessToken)).StatusCode);

            Assert.True((await _helpers.GetMyProfileResultAsync(accessToken)).IsEmailConfirmed);
        }

        [Fact]
        public async Task ResendEmailConfirmation_ForAConfirmedUser_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("resend-confirmed");

            var response = await _helpers.ResendEmailConfirmationAsync(accessToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "Email is already confirmed");
        }

        [Fact]
        public async Task ResendEmailConfirmation_WithoutAccessToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _helpers.ResendEmailConfirmationAsync()).StatusCode);
        }
    }
}
