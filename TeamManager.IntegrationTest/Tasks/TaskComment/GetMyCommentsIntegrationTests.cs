using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskComment
{
    [Collection("Projects")]
    public sealed class GetMyCommentsIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task GetMyComments_ReturnsOnlyTheCommentsWrittenByTheCurrentUserAcrossProjects()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mycomments");
            var (authorId, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Feed Author", ownerToken);
            var (_, otherAuthorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Other Author", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "Feed task one", ownerToken);
            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Feed task two", ownerToken);

            var ownTaskOneCommentId = await _helpers.AddTaskCommentAsync(firstTaskId, "Written by me here", authorToken);
            await _helpers.AddTaskCommentAsync(secondTaskId, "Written by someone else", otherAuthorToken);

            var otherProjectId = (await _helpers.CreateProjectAsync($"Project {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;
            await _helpers.AddProjectMemberAsync(otherProjectId, authorId, ProjectRole.Member, ownerToken);

            var otherProjectTaskId = await _helpers.CreateTaskAsync(otherProjectId, "Feed task three", ownerToken);
            var ownOtherProjectCommentId = await _helpers.AddTaskCommentAsync(otherProjectTaskId, "Written by me there", authorToken);

            var feed = await _helpers.GetMyCommentsResultAsync(authorToken);

            Assert.Equal(new[] { ownTaskOneCommentId, ownOtherProjectCommentId }, feed.Items.Select(x => x.Id).OrderBy(x => x).ToArray());
            Assert.Equal(2, feed.TotalCount);
            Assert.Equal(1, feed.Page);
            Assert.Equal(20, feed.PageSize);
            Assert.Equal(new[] { "Feed task one", "Feed task three" },
                feed.Items.Select(x => x.TaskTitle).OrderBy(x => x).ToArray());

            var otherFeed = await _helpers.GetMyCommentsResultAsync(otherAuthorToken);

            Assert.Equal("Written by someone else", Assert.Single(otherFeed.Items).Content);
        }

        [Fact]
        public async Task GetMyComments_ReturnsTheNewestCommentsFirst()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mycomments-order");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Ordered feed task", ownerToken);

            await _helpers.AddTaskCommentAsync(taskId, "Earliest written", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Middle written", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Latest written", ownerToken);

            var feed = await _helpers.GetMyCommentsResultAsync(ownerToken);

            var created = feed.Items.Select(x => x.CreatedAtUtc).ToArray();

            Assert.Equal(created.OrderByDescending(x => x).ToArray(), created);
        }

        [Fact]
        public async Task GetMyComments_PagesThroughTheFeed()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mycomments-paging");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Paged feed task", ownerToken);

            var ids = new List<long>();

            for (var index = 1; index <= 3; index++)
                ids.Add(await _helpers.AddTaskCommentAsync(taskId, $"Feed note {index}", ownerToken));

            var firstPage = await _helpers.GetMyCommentsResultAsync(ownerToken, "page=1&pageSize=2");
            var secondPage = await _helpers.GetMyCommentsResultAsync(ownerToken, "page=2&pageSize=2");

            Assert.Equal(2, firstPage.Items.Count);
            Assert.Single(secondPage.Items);
            Assert.Equal(3, firstPage.TotalCount);
            Assert.Equal(3, secondPage.TotalCount);
            Assert.Equal(2, secondPage.Page);

            Assert.Equal(ids, firstPage.Items.Concat(secondPage.Items).Select(x => x.Id).OrderBy(x => x).ToArray());
            Assert.Empty(firstPage.Items.Select(x => x.Id).Intersect(secondPage.Items.Select(x => x.Id)));
        }

        [Fact]
        public async Task GetMyComments_ExposesTheMentionsOfTheComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mycomments-mentions");
            var (mentionedId, _) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Mentioned Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Mentioned feed task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Pinging the admin", ownerToken, new[] { mentionedId });

            var comment = Assert.Single((await _helpers.GetMyCommentsResultAsync(ownerToken)).Items);

            var mention = Assert.Single(comment.Mentions);

            Assert.Equal(mentionedId, mention.UserId);
            Assert.Equal("Mentioned Admin", mention.UserName);
        }

        [Fact]
        public async Task GetMyComments_AfterTheTaskIsDeleted_ExcludesTheComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mycomments-deletedtask");
            var (_, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Deleted Task Author", ownerToken);

            var doomedTaskId = await _helpers.CreateTaskAsync(projectId, "Doomed feed task", ownerToken);
            var keptTaskId = await _helpers.CreateTaskAsync(projectId, "Kept feed task", ownerToken);

            await _helpers.AddTaskCommentAsync(doomedTaskId, "Vanishes with the task", authorToken);
            var keptCommentId = await _helpers.AddTaskCommentAsync(keptTaskId, "Stays in the feed", authorToken);

            await _helpers.DeleteTaskAsync(doomedTaskId, ownerToken);

            Assert.Equal(keptCommentId, Assert.Single((await _helpers.GetMyCommentsResultAsync(authorToken)).Items).Id);
        }

        [Fact]
        public async Task GetMyComments_AfterTheProjectIsDeleted_ExcludesTheComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mycomments-deletedproject");
            var (authorId, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Deleted Project Author", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed project feed task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Vanishes with the project", authorToken);

            // Only an inactive task blocks a project from deleting, and a cancelled task keeps its comments.
            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Cancelled, ownerToken)).StatusCode);

            var otherProjectId = (await _helpers.CreateProjectAsync($"Project {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;
            await _helpers.AddProjectMemberAsync(otherProjectId, authorId, ProjectRole.Member, ownerToken);

            var keptTaskId = await _helpers.CreateTaskAsync(otherProjectId, "Kept project feed task", ownerToken);
            var keptCommentId = await _helpers.AddTaskCommentAsync(keptTaskId, "Stays in the feed", authorToken);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            Assert.Equal(keptCommentId, Assert.Single((await _helpers.GetMyCommentsResultAsync(authorToken)).Items).Id);
        }

        [Fact]
        public async Task GetMyComments_AfterBeingRemovedFromTheProject_StillListsTheComment()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mycomments-removed");
            var (authorId, _, authorToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-mycomments-removed-author");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, authorId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var membershipId = await _helpers.AddProjectMemberAsync(projectId, authorId, ProjectRole.Member, ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Left project feed task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Written before leaving", authorToken);

            await _helpers.RemoveProjectMemberAsync(projectId, membershipId, ownerToken);

            // The feed filters by the author alone, so the comment and the title of the project the user left stay readable.
            var comment = Assert.Single((await _helpers.GetMyCommentsResultAsync(authorToken)).Items);

            Assert.Equal(commentId, comment.Id);
            Assert.Equal("Left project feed task", comment.TaskTitle);
        }

        [Fact]
        public async Task GetMyComments_WithoutAccessToken_Returns401()
        {
            var response = await _helpers.GetMyCommentsAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


    }
}
