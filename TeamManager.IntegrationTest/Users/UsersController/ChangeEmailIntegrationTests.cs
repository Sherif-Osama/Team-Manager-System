using System.Net;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Users.Configuration;

namespace TeamManager.IntegrationTest.Users.UsersController
{
    [Collection("Common")]
    public sealed class ChangeEmailIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly UserTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task ChangeEmail_WithValidPasswordAndNewEmail_Returns204AndKeepsTheCurrentEmailUntilConfirmation()
        {
            var (_, email, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("email-change");
            var newEmail = $"email-changed-{Guid.NewGuid():N}@example.com";

            var response = await _helpers.ChangeEmailRequestAsync(new { currentPassword = "P@ssword1234", newEmail },
                accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var profile = await _helpers.GetMyProfileResultAsync(accessToken);

            Assert.Equal(email, profile.Email);
            await _authenticatedUser.LoginAsync(email);
        }

        [Fact]
        public async Task ChangeEmail_EmitsAConfirmationEmailToTheNewAddress()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("email-outbox");
            var newEmail = $"email-outbox-{Guid.NewGuid():N}@example.com";

            await _helpers.ChangeEmailRequestAsync(new { currentPassword = "P@ssword1234", newEmail }, accessToken);

            Assert.Equal(1, await _helpers.CountEmailConfirmationMessagesAsync(newEmail));
            Assert.NotNull(await _helpers.GetLatestEmailConfirmationTokenAsync(newEmail));
        }

        [Fact]
        public async Task ChangeEmail_WithWrongPassword_Returns401()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("email-wrongpassword");

            var response = await _helpers.ChangeEmailRequestAsync(
                new { currentPassword = "WrongPassword123", newEmail = $"wrong-{Guid.NewGuid():N}@example.com" }, accessToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeEmail_WithAnEmailTakenByAnotherUser_Returns409()
        {
            var (_, takenEmail, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("email-taken");
            var (_, _, secondToken) = await _authenticatedUser.CreateConfirmedUserAsync("email-taker");

            var response = await _helpers.ChangeEmailRequestAsync(new { currentPassword = "P@ssword1234", newEmail = takenEmail },
                secondToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task ChangeEmail_WithTheCurrentEmail_Returns400()
        {
            var (_, email, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("email-same");

            var response = await _helpers.ChangeEmailRequestAsync(new { currentPassword = "P@ssword1234", newEmail = email },
                accessToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeEmail_WithAnInvalidEmailFormat_Returns400()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("email-invalid");

            var response = await _helpers.ChangeEmailRequestAsync(
                new { currentPassword = "P@ssword1234", newEmail = "not-an-email" }, accessToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "NewEmail");
        }

        [Fact]
        public async Task ChangeEmail_WithoutAccessToken_Returns401()
        {
            var response = await _helpers.ChangeEmailRequestAsync(
                new { currentPassword = "P@ssword1234", newEmail = $"notoken-{Guid.NewGuid():N}@example.com" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
