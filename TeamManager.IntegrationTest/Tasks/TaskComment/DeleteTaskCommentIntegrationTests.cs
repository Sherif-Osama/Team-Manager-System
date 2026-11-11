using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Task;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskComment
{
    [Collection("Tasks")]
    public sealed class DeleteTaskCommentIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task DeleteTaskComment_AsTheAuthor_Returns204AndRemovesTheCommentWithItsMentions()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete");
            var (authorId, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Deleting Author", ownerToken);
            var (mentionedId, mentionedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Admin, "Deleted Mention", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Deleted comment task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Here for a moment", authorToken, [mentionedId]);

            Assert.NotEmpty((await _helpers.GetMyMentionsResultAsync(mentionedToken, "page=1&pageSize=100")).Items);

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, commentId, authorToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.Empty((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);
            Assert.Empty((await _helpers.GetMyMentionsResultAsync(mentionedToken, "page=1&pageSize=100")).Items);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(0, task.CommentsCount);
        }

        [Fact]
        public async Task DeleteTaskComment_AsProjectAdminOrOwner_RemovesAnotherUsersComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-staff");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Deleting Admin", ownerToken);
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Deleted Author", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Staff deletes task", ownerToken);

            var adminTargetId = await _helpers.AddTaskCommentAsync(taskId, "Removed by the admin", memberToken);
            var ownerTargetId = await _helpers.AddTaskCommentAsync(taskId, "Removed by the owner", memberToken);

            var asAdmin = await _helpers.DeleteTaskCommentRequestAsync(taskId, adminTargetId, adminToken);
            var asOwner = await _helpers.DeleteTaskCommentRequestAsync(taskId, ownerTargetId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, asAdmin.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, asOwner.StatusCode);

            Assert.Empty((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);
        }

        [Fact]
        public async Task DeleteTaskComment_AsProjectMemberWhoIsNotTheAuthor_Returns403AndKeepsTheComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-stranger-member");
            var (_, otherMemberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Other Member", ownerToken);
            var (_, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Protected Author", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Member deletes task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Protected by its author", authorToken);

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, commentId, otherMemberToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Forbidden, "only delete your own comments");

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal(commentId, comment.Id);
        }

        [Fact]
        public async Task DeleteTaskComment_AsProjectViewer_Returns403AndKeepsTheComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-viewer");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Deleting Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Viewer deletes task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Out of a viewer reach", ownerToken);

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, commentId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);
        }

        [Fact]
        public async Task DeleteTaskComment_AsTeamOwnerWithoutProjectMembership_Returns403AndKeepsTheComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-delete-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner deletes task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Written by the project owner", ownerToken);

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, commentId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal(commentId, comment.Id);
        }

        [Fact]
        public async Task DeleteTaskComment_AsTeamOwnerWhoWroteTheComment_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-teamownerown");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-delete-teamownerown-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner owns comment task", ownerToken);

            var add = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Written by the team owner" }, teamOwnerToken);

            Assert.Equal(HttpStatusCode.OK, add.StatusCode);

            var commentId = await add.Content.ReadFromJsonAsync<long>();

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, commentId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.Empty((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);
        }

        [Fact]
        public async Task DeleteTaskComment_ForACommentOfAnotherTask_Returns404AndKeepsTheOtherComment()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-foreign");

            var otherTaskId = await _helpers.CreateTaskAsync(projectId, "Comment owner task", ownerToken);
            var foreignCommentId = await _helpers.AddTaskCommentAsync(otherTaskId, "Belongs to another task", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Delete foreign task", ownerToken);

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, foreignCommentId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            Assert.Single((await _helpers.GetTaskCommentsResultAsync(otherTaskId, ownerToken)).Items);
        }

        [Fact]
        public async Task DeleteTaskComment_ForUnknownTask_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-unknowntask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Unknown task delete", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Orphan delete target", ownerToken);

            var response = await _helpers.DeleteTaskCommentRequestAsync(long.MaxValue, commentId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskComment_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed delete task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Deleted task comment", ownerToken);

            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, commentId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskComment_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-delete-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token delete task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "No token delete", ownerToken);

            var response = await _helpers.DeleteTaskCommentRequestAsync(taskId, commentId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


    }
}
