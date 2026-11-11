using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Notifications.Configuration;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Notifications
{
    [Collection("Projects")]
    public sealed class NotificationGenerationIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly NotificationTestHelpers _notifications = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task AssigningATask_CreatesTheTaskAssignedNotificationForTheAssignee()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-assign");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Assigned Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Assigned notification task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(NotificationType.TaskAssigned, item.Type);
            Assert.Equal("Task Assigned", item.Title);
            Assert.Equal("You have been assigned to a task.", item.Body);
            Assert.Equal("Task", item.RelatedEntityType);
            Assert.Equal(taskId.ToString(), item.RelatedEntityId);
            Assert.False(item.IsRead);
            Assert.Null(item.ReadAtUtc);

            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(memberToken));
            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(ownerToken));

            Assert.Empty((await _notifications.GetNotificationsResultAsync(ownerToken)).Items);
        }

        [Fact]
        public async Task AssigningATaskToOneself_CreatesNoNotification()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("gen-selfassign-owner");
            var (_, projectId) = await _helpers.CreateProjectAsync("Self assign project", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Self assigned task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, ownerId, ownerToken);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(ownerToken)).Items);
        }

        [Fact]
        public async Task BeingMentionedInAComment_CreatesTheMentionNotification()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-mention");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Mentioned Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Mentioned notification task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Please take a look", ownerToken, new[] { memberId });

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(NotificationType.Mention, item.Type);
            Assert.Equal("You were mentioned", item.Title);
            Assert.Equal("You were mentioned in a task comment.", item.Body);
            Assert.Equal("TaskComment", item.RelatedEntityType);
            Assert.Equal(commentId.ToString(), item.RelatedEntityId);
        }

        [Fact]
        public async Task MentioningTheAssignee_SendsTheMentionNotificationOnly()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-mention-assignee");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Assignee Mentioned", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Assignee mention task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "You are already on this", ownerToken, new[] { memberId });

            var types = (await _notifications.GetNotificationsResultAsync(memberToken)).Items
                .Select(x => x.Type).OrderBy(x => x).ToArray();

            Assert.Equal(new[] { NotificationType.TaskAssigned, NotificationType.Mention }, types);
        }

        [Fact]
        public async Task CommentingOnAnAssignedTask_CreatesTheCommentAddedNotificationForTheAssignee()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-comment");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Commented Assignee", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Comment notification task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Status update for the assignee", ownerToken);

            var comment = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items
                .Where(x => x.Type == NotificationType.CommentAdded).ToArray());

            Assert.Equal("New Comment", comment.Title);
            Assert.Equal("A new comment was added to your task.", comment.Body);
            Assert.Equal("TaskComment", comment.RelatedEntityType);
            Assert.Equal(commentId.ToString(), comment.RelatedEntityId);
        }

        [Fact]
        public async Task CommentingOnATaskWithoutAnAssignee_CreatesNoNotification()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-nocomment");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Unassigned comment task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Nobody is assigned here", ownerToken);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(ownerToken)).Items);
        }

        [Fact]
        public async Task TheAssigneeCommentingOnHisOwnTask_DoesNotNotifyHimself()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-owncomment");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Self Commenter", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Own comment task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Note to myself", memberToken);

            var types = (await _notifications.GetNotificationsResultAsync(memberToken)).Items
                .Select(x => x.Type).ToArray();

            Assert.Equal(new[] { NotificationType.TaskAssigned }, types);
        }

        [Fact]
        public async Task CompletingSomeoneElsesTask_CreatesTheTaskCompletedNotificationForTheCreator()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-complete");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Completing Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Completed notification task", ownerToken);
            await _helpers.CompleteTaskAsync(taskId, adminToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(ownerToken)).Items);

            Assert.Equal(NotificationType.TaskCompleted, item.Type);
            Assert.Equal("Task Completed", item.Title);
            Assert.Equal("A task you created has been completed.", item.Body);
            Assert.Equal("Task", item.RelatedEntityType);
            Assert.Equal(taskId.ToString(), item.RelatedEntityId);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(adminToken)).Items);
        }

        [Fact]
        public async Task CompletingYourOwnTask_CreatesNoNotification()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-owncomplete");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Self completed task", ownerToken);
            await _helpers.CompleteTaskAsync(taskId, ownerToken);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(ownerToken)).Items);
        }

        [Fact]
        public async Task MovingSomeoneElsesTaskThroughTheWorkflowWithoutFinishingIt_CreatesNoNotification()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-inreview");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Working Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Started but not done task", ownerToken);
            await _helpers.MoveTaskThroughWorkflowAsync(taskId, adminToken);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(ownerToken)).Items);
            Assert.Empty((await _notifications.GetNotificationsResultAsync(adminToken)).Items);
        }

        [Fact]
        public async Task InvitingARegisteredUser_CreatesTheTeamInvitationNotification()
        {
            var (teamId, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-invite");
            var (_, invitedEmail, invitedToken) = await _authenticatedUser.CreateConfirmedUserAsync("gen-invited");

            var invitationId = await _notifications.InviteRegisteredUserAsync(teamId, invitedEmail, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(invitedToken)).Items);

            Assert.Equal(NotificationType.TeamInvitation, item.Type);
            Assert.Equal("New Team Invitation", item.Title);
            Assert.Equal("You have received a new team invitation.", item.Body);
            Assert.Equal("TeamInvitation", item.RelatedEntityType);
            Assert.Equal(invitationId.ToString(), item.RelatedEntityId);
        }

        [Fact]
        public async Task InvitingAnEmailThatHasNoAccount_CreatesNoNotification()
        {
            var (teamId, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-invite-unknown");

            var invitationId = await _notifications.InviteRegisteredUserAsync(teamId,
                $"nobody-{Guid.NewGuid():N}@example.com", ownerToken);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.False(await db.Notifications.AnyAsync(x => x.RelatedEntityId == invitationId.ToString()));
        }

        [Fact]
        public async Task AssignmentsAndMentionsAndCommentsOfOneUser_AppearsInOneFeedAcrossProjects()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-mixed");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Mixed Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Mixed first task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Pinged", ownerToken, new[] { memberId });

            var (_, secondProjectId) = await _helpers.CreateProjectAsync("Second mixed project", ownerToken, teamId: teamId);
            await _helpers.AddProjectMemberAsync(secondProjectId, memberId, ProjectRole.Member, ownerToken);

            var otherTaskId = await _helpers.CreateTaskAsync(secondProjectId, "Mixed second task", ownerToken);
            await _helpers.AssignTaskAsync(otherTaskId, memberId, ownerToken);
            await _helpers.AddTaskCommentAsync(otherTaskId, "Plain comment", ownerToken);

            var result = await _notifications.GetNotificationsResultAsync(memberToken);

            var types = result.Items.Select(x => x.Type).OrderBy(x => x).ToArray();

            Assert.Equal(new[]
            {
                NotificationType.TaskAssigned, NotificationType.TaskAssigned, NotificationType.Mention,
                NotificationType.CommentAdded
            }, types);
            Assert.Equal(4, result.TotalCount);

            Assert.Equal(4, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task RemovingTheRecipientFromTheProject_LeavesHisNotificationsInPlace()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("gen-removed");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Removed Recipient", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Task before removal", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var notification = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            await _helpers.RemoveProjectMemberAsync(projectId, await _helpers.GetProjectMemberIdAsync(projectId, memberId), ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(notification.Id, item.Id);
        }
    }
}
