using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Task;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskComment
{
    [Collection("Tasks")]
    public sealed class EditTaskCommentIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task EditTaskComment_AsTheAuthor_Returns204AndReplacesTheContent()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Edited task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Original text", ownerToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, commentId,
                new { content = "Corrected text" }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal(commentId, comment.Id);
            Assert.Equal("Corrected text", comment.Content);
            Assert.NotNull(comment.UpdatedAtUtc);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(1, task.CommentsCount);
        }

        [Fact]
        public async Task EditTaskComment_WithoutTheMentionsField_KeepsTheExistingMentions()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-keepmentions");
            var (mentionedId, _) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Kept Mention", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Keep mentions task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Ping", ownerToken, [mentionedId]);

            await _helpers.EditTaskCommentAsync(taskId, commentId, "Ping again", ownerToken);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal(new[] { mentionedId }, comment.Mentions.Select(x => x.UserId).ToArray());
        }

        [Fact]
        public async Task EditTaskComment_ReplacingTheMentions_StoresTheNewListOnly()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-mentions");
            var (firstMentionId, _) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "First Mention", ownerToken);
            var (secondMentionId, _) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Second Mention", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Replaced mentions task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "First list", ownerToken, [firstMentionId]);

            await _helpers.EditTaskCommentAsync(taskId, commentId, "Second list", ownerToken, [secondMentionId]);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal("Second list", comment.Content);
            Assert.Equal(new[] { secondMentionId }, comment.Mentions.Select(x => x.UserId).ToArray());
        }

        [Fact]
        public async Task EditTaskComment_AsProjectOwnerWhoIsNotTheAuthor_Returns403AndKeepsTheContent()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-owner");
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Comment Author", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Owner edits task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Written by a member", memberToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, commentId,
                new { content = "Rewritten by the owner" }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Forbidden, "Only the author can edit");

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal("Written by a member", comment.Content);
        }

        [Fact]
        public async Task EditTaskComment_AsProjectAdminWhoIsNotTheAuthor_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-admin");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Editing Admin", ownerToken);
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Comment Author", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Admin edits task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Written by a member", memberToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, commentId,
                new { content = "Rewritten by the admin" }, adminToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Forbidden, "Only the author can edit");
        }

        [Fact]
        public async Task EditTaskComment_AsTeamOwnerWithoutProjectMembership_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-edit-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner edits task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Written by the project owner", ownerToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, commentId,
                new { content = "Rewritten by the team owner" }, teamOwnerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task EditTaskComment_ForACommentOfAnotherTask_Returns404AndKeepsTheOtherComment()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-foreign");

            var otherTaskId = await _helpers.CreateTaskAsync(projectId, "Comment owner task", ownerToken);
            var foreignCommentId = await _helpers.AddTaskCommentAsync(otherTaskId, "Belongs to another task", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Edit foreign task", ownerToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, foreignCommentId,
                new { content = "Rewritten from the wrong task" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var otherComment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(otherTaskId, ownerToken)).Items);

            Assert.Equal("Belongs to another task", otherComment.Content);
            Assert.Null(otherComment.UpdatedAtUtc);
        }

        [Fact]
        public async Task EditTaskComment_MentionsAUserWhoIsNotAMemberOfTheProject_Returns404AndKeepsTheComment()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-badmention");
            var (strangerId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-edit-badmention-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Bad mention edit task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Original text", ownerToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, commentId,
                new { content = "Rewritten text", mentionedUserIds = new[] { strangerId } }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.NotFound, "is not an active member");

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal("Original text", comment.Content);
            Assert.Empty(comment.Mentions);
        }

        [Fact]
        public async Task EditTaskComment_ForUnknownTask_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-unknowntask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Unknown task edit", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Orphan edit target", ownerToken);

            var response = await _helpers.EditTaskCommentRequestAsync(long.MaxValue, commentId,
                new { content = "Rewritten" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task EditTaskComment_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed edit task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Deleted task comment", ownerToken);

            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, commentId, new { content = "Rewritten" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task EditTaskComment_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-edit-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token edit task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "No token edit", ownerToken);

            var response = await _helpers.EditTaskCommentRequestAsync(taskId, commentId, new { content = "Rewritten" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }




    }
}
