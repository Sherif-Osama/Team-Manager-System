using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskComment
{
    [Collection("Common")]
    public sealed class AddTaskCommentIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task AddTaskComment_AsProjectOwner_ReturnsTheCommentIdAndStoresIt()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Commented task", ownerToken);

            var commentId = await _helpers.AddTaskCommentAsync(taskId, "First comment", ownerToken);

            Assert.True(commentId > 0);

            var listed = await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken);

            var comment = Assert.Single(listed.Items);

            Assert.Equal(commentId, comment.Id);
            Assert.Equal("First comment", comment.Content);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, comment.AuthorUserName);
            Assert.Null(comment.UpdatedAtUtc);
            Assert.Empty(comment.Mentions);
            Assert.Equal(1, listed.TotalCount);
            Assert.Equal(1, listed.Page);
            Assert.Equal(20, listed.PageSize);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(1, task.CommentsCount);
            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Fact]
        public async Task AddTaskComment_WithMentions_StoresTheMentionedUsers()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-mention");
            var (mentionedId, _) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Mentioned User", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Mention task", ownerToken);

            await _helpers.AddTaskCommentAsync(taskId, "Ping", ownerToken, [mentionedId]);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            var mention = Assert.Single(comment.Mentions);

            Assert.Equal(mentionedId, mention.UserId);
            Assert.Equal("Mentioned User", mention.UserName);
        }

        [Fact]
        public async Task AddTaskComment_MentioningMyself_StoresNoMention()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-selfmention");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Self Mentioner", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Self mention task", ownerToken);

            var commentId = await _helpers.AddTaskCommentAsync(taskId, "I mention myself", memberToken, [memberId]);

            Assert.True(commentId > 0);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Empty(comment.Mentions);
        }

        [Fact]
        public async Task AddTaskComment_AsProjectAdminOrMember_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-roles");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Comment Admin", ownerToken);
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Comment Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Roles comment task", ownerToken);

            var asAdmin = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Admin comment" }, adminToken);
            var asMember = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Member comment" }, memberToken);

            Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
            Assert.Equal(HttpStatusCode.OK, asMember.StatusCode);

            var listed = await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken);

            Assert.Equal(new[] { "Admin comment", "Member comment" }, listed.Items.Select(x => x.Content).ToArray());
        }

        [Fact]
        public async Task AddTaskComment_AsProjectViewer_Returns403AndStoresNothing()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-viewer");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Comment Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Viewer comment task", ownerToken);

            var response = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Viewer comment" }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Empty((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);
        }

        [Fact]
        public async Task AddTaskComment_AsTeamMemberWithoutProjectMembership_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-teammember");
            var (teamMemberId, _, teamMemberToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-add-teammember-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamMemberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team member comment task", ownerToken);

            var response = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Team member comment" }, teamMemberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.Empty((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);
        }

        [Fact]
        public async Task AddTaskComment_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-add-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Outsider comment task", ownerToken);

            var response = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Outsider comment" }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskComment_AsTeamOwnerWithoutProjectMembership_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-add-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner comment task", ownerToken);

            var response = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Team owner comment" }, teamOwnerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskComment_MentionsAUserWhoIsNotAMemberOfTheProject_Returns404AndStoresNothing()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-badmention");
            var (strangerId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-add-badmention-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Bad mention task", ownerToken);

            var response = await _helpers.AddTaskCommentRequestAsync(taskId,
                new { content = "Mentioning a stranger", mentionedUserIds = new[] { strangerId } }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.NotFound, "is not an active member");

            Assert.Empty((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);
        }


        [Fact]
        public async Task AddTaskComment_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-unknowntask");

            var response = await _helpers.AddTaskCommentRequestAsync(long.MaxValue, new { content = "Comment" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskComment_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed comment task", ownerToken);

            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "Comment" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskComment_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token comment task", ownerToken);

            var response = await _helpers.AddTaskCommentRequestAsync(taskId, new { content = "No token comment" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }




        [Fact]
        public async Task AddTaskComment_WithTwoThousandCharacters_Returns200()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-add-maxcontent");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Boundary comment task", ownerToken);

            var commentId = await _helpers.AddTaskCommentAsync(taskId, new string('a', 2000), ownerToken);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal(commentId, comment.Id);
            Assert.Equal(2000, comment.Content.Length);
        }
    }
}
