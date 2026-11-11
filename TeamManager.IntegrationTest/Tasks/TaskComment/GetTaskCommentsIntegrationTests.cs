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
    public sealed class GetTaskCommentsIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task GetTaskComments_AsProjectViewer_ReturnsThatTaskCommentsOldestFirst()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get");
            var (authorId, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Comment Author", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Commented task", ownerToken);
            var otherTaskId = await _helpers.CreateTaskAsync(projectId, "Neighbouring task", ownerToken);

            var firstId = await _helpers.AddTaskCommentAsync(taskId, "First note", authorToken);
            var secondId = await _helpers.AddTaskCommentAsync(taskId, "Second note", ownerToken);
            await _helpers.AddTaskCommentAsync(otherTaskId, "Belongs to another task", ownerToken);

            var result = await _helpers.GetTaskCommentsResultAsync(taskId, authorToken);

            Assert.Equal(new[] { firstId, secondId }, result.Items.Select(x => x.Id).ToArray());
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);

            var ownComment = Assert.Single(result.Items, x => x.Id == firstId);

            Assert.Equal(authorId, ownComment.AuthorUserId);
            Assert.Equal("Comment Author", ownComment.AuthorUserName);
            Assert.Equal("First note", ownComment.Content);
            Assert.Empty(ownComment.Mentions);
        }

        [Fact]
        public async Task GetTaskComments_ForATaskWithoutComments_ReturnsAnEmptyListing()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-empty");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Silent task", ownerToken);

            var result = await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetTaskComments_ExposesTheMentionedUsers()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-mentions");
            var (mentionedId, _) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Mentioned Viewer", ownerToken);
            var (authorId, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Mentioning Author", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Mention listing task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Ping for the viewer", authorToken, new[] { mentionedId });

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            var mention = Assert.Single(comment.Mentions);

            Assert.Equal(mentionedId, mention.UserId);
            Assert.Equal("Mentioned Viewer", mention.UserName);
            Assert.Equal(authorId, comment.AuthorUserId);
        }

        [Fact]
        public async Task GetTaskComments_PagesThroughTheComments()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-paging");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Paged comments task", ownerToken);

            var ids = new List<long>();

            for (var index = 1; index <= 3; index++)
                ids.Add(await _helpers.AddTaskCommentAsync(taskId, $"Note {index}", ownerToken));

            var firstPage = await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken, "page=1&pageSize=2");
            var secondPage = await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken, "page=2&pageSize=2");

            Assert.Equal(new[] { ids[0], ids[1] }, firstPage.Items.Select(x => x.Id).ToArray());
            Assert.Equal(ids[2], Assert.Single(secondPage.Items).Id);

            Assert.Equal(3, firstPage.TotalCount);
            Assert.Equal(3, secondPage.TotalCount);
            Assert.Equal(2, firstPage.PageSize);
            Assert.Equal(2, secondPage.Page);

            Assert.Empty(firstPage.Items.Select(x => x.Id).Intersect(secondPage.Items.Select(x => x.Id)));
        }

        [Fact]
        public async Task GetTaskComments_OrdersByCreationDate()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-order");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Ordered comments task", ownerToken);

            await _helpers.AddTaskCommentAsync(taskId, "Earliest", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Middle", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Latest", ownerToken);

            var created = (await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items.Select(x => x.CreatedAtUtc).ToArray();

            Assert.Equal(created.OrderBy(x => x).ToArray(), created);
        }

        [Fact]
        public async Task GetTaskComments_WithoutAProjectRole_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-roles");
            var (teamMemberId, _, teamMemberToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-get-teammember");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamMemberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-get-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Locked comments task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Hidden from outsiders", ownerToken);

            var asTeamMember = await _helpers.GetTaskCommentsAsync(taskId, teamMemberToken);
            var asStranger = await _helpers.GetTaskCommentsAsync(taskId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asTeamMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asStranger.StatusCode);
        }

        [Fact]
        public async Task GetTaskComments_AsTeamOwnerWithoutProjectMembership_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-get-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner comments task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Visible to the team owner", ownerToken);

            Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, teamOwnerToken)).Items);
        }

        [Fact]
        public async Task GetTaskComments_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token comments task", ownerToken);

            var response = await _helpers.GetTaskCommentsAsync(taskId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskComments_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-unknowntask");

            var response = await _helpers.GetTaskCommentsAsync(long.MaxValue, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskComments_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-get-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed comments task", ownerToken);

            await _helpers.AddTaskCommentAsync(taskId, "Row under a deleted task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            // The query itself has no deleted-task filter, so the task authorization behavior is the only guard here.
            var response = await _helpers.GetTaskCommentsAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }



    }
}
