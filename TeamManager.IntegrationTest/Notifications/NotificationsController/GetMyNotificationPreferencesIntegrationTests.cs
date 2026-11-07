using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Notifications.Configuration;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Notifications.NotificationsController
{
    [Collection("Common")]
    public sealed class GetMyNotificationPreferencesIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly NotificationTestHelpers _notifications = new(factory);

        [Fact]
        public async Task GetMyNotificationPreferences_ForAUserThatNeverChangedAnything_ReturnsEveryTypeEnabled()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("prefs-default");

            var result = await _notifications.GetPreferencesResultAsync(ownerToken);

            Assert.Equal(5, result.Items.Count);
            Assert.All(result.Items, x => Assert.True(x.IsEnabled));
        }

        [Fact]
        public async Task GetMyNotificationPreferences_ListsTheTypesInDeclarationOrder()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("prefs-order");

            var types = (await _notifications.GetPreferencesResultAsync(ownerToken)).Items
                .Select(x => x.NotificationType).ToArray();

            Assert.Equal(new[]
            {
                NotificationType.TaskAssigned, NotificationType.Mention, NotificationType.TeamInvitation,
                NotificationType.CommentAdded, NotificationType.TaskCompleted
            }, types);
        }

        [Fact]
        public async Task GetMyNotificationPreferences_ReflectsADisabledTypeAndKeepsTheRestEnabled()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("prefs-disabled");

            await _notifications.UpdatePreferencesAsync(ownerToken, (NotificationType.Mention, false));

            var items = (await _notifications.GetPreferencesResultAsync(ownerToken)).Items.ToArray();

            Assert.Equal(5, items.Length);
            Assert.False(items.Single(x => x.NotificationType == NotificationType.Mention).IsEnabled);
            Assert.True(items.Single(x => x.NotificationType == NotificationType.TaskAssigned).IsEnabled);
            Assert.True(items.Single(x => x.NotificationType == NotificationType.TeamInvitation).IsEnabled);
            Assert.True(items.Single(x => x.NotificationType == NotificationType.CommentAdded).IsEnabled);
            Assert.True(items.Single(x => x.NotificationType == NotificationType.TaskCompleted).IsEnabled);
        }

        [Fact]
        public async Task GetMyNotificationPreferences_AfterEveryTypeIsDisabled_ReturnsFiveDisabledItems()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("prefs-allof");

            await _notifications.UpdatePreferencesAsync(ownerToken,
                (NotificationType.TaskAssigned, false), (NotificationType.Mention, false), (NotificationType.TeamInvitation, false),
                (NotificationType.CommentAdded, false), (NotificationType.TaskCompleted, false));

            var result = await _notifications.GetPreferencesResultAsync(ownerToken);

            Assert.Equal(5, result.Items.Count);
            Assert.All(result.Items, x => Assert.False(x.IsEnabled));
        }

        [Fact]
        public async Task GetMyNotificationPreferences_IsPerUser()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("prefs-peruser");
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Preferences Member", ownerToken);

            await _notifications.UpdatePreferencesAsync(memberToken, (NotificationType.Mention, false));

            Assert.False((await _notifications.GetPreferencesResultAsync(memberToken)).Items
                .Single(x => x.NotificationType == NotificationType.Mention).IsEnabled);

            Assert.True((await _notifications.GetPreferencesResultAsync(ownerToken)).Items
                .Single(x => x.NotificationType == NotificationType.Mention).IsEnabled);
        }

        [Fact]
        public async Task GetMyNotificationPreferences_EnvelopeCarriesOnlyTheItemsArray()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("prefs-envelope");

            var body = await (await _notifications.GetPreferencesAsync(ownerToken)).Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(JsonValueKind.Object, body.ValueKind);
            Assert.Equal(new[] { "items" }, body.EnumerateObject().Select(x => x.Name).ToArray());
        }

        [Fact]
        public async Task GetMyNotificationPreferences_SendsTheTypeAsANumber()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("prefs-number");

            var body = await (await _notifications.GetPreferencesAsync(ownerToken)).Content.ReadFromJsonAsync<JsonElement>();

            var first = body.GetProperty("items")[0];

            Assert.Equal(JsonValueKind.Number, first.GetProperty("notificationType").ValueKind);
            Assert.Equal(1, first.GetProperty("notificationType").GetInt32());
            Assert.Equal(JsonValueKind.True, first.GetProperty("isEnabled").ValueKind);
        }

        [Fact]
        public async Task GetMyNotificationPreferences_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _notifications.GetPreferencesAsync()).StatusCode);
        }
    }
}
