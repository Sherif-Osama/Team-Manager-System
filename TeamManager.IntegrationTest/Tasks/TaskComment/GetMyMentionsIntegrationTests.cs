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
    public sealed class GetMyMentionsIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task GetMyMentions_AsTheMentionedUser_ReturnsTheCommentWithItsAuthor()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions");
            var (authorId, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Mentioning Author", ownerToken);
            var (mentionedId, mentionedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Viewer, "Mentioned Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Mentions feed task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Need a review please", authorToken, new[] { mentionedId });

            var result = await _helpers.GetMyMentionsResultAsync(mentionedToken);

            var mention = Assert.Single(result.Items);

            Assert.Equal(commentId, mention.CommentId);
            Assert.Equal(taskId, mention.TaskId);
            Assert.Equal("Mentions feed task", mention.TaskTitle);
            Assert.Equal(authorId, mention.AuthorUserId);
            Assert.Equal("Mentioning Author", mention.AuthorUserName);
            Assert.Equal("Need a review please", mention.Content);
            Assert.Null(mention.UpdatedAtUtc);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task GetMyMentions_ExposesTheTaskTitleAndTheAuthorName()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-shape");
            var (authorId, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Named Author", ownerToken);
            var (mentionedId, mentionedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Viewer, "Mentioned With Names", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Titled mention task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Pinged with a title", authorToken, new[] { mentionedId });

            var mention = Assert.Single((await _helpers.GetMyMentionsResultAsync(mentionedToken)).Items);

            Assert.Equal("Titled mention task", mention.TaskTitle);
            Assert.Equal("Named Author", mention.AuthorUserName);
            Assert.Equal(authorId, mention.AuthorUserId);
        }

        [Fact]
        public async Task GetMyMentions_DoesNotListMentionsAddressedToSomeoneElse()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-other");
            var (addressedId, addressedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Member, "Addressed Member", ownerToken);
            var (_, bystanderToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Bystander Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private mention task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Only one person pinged", bystanderToken,
                new[] { addressedId });

            Assert.Empty((await _helpers.GetMyMentionsResultAsync(bystanderToken)).Items);
            Assert.Equal(commentId, Assert.Single((await _helpers.GetMyMentionsResultAsync(addressedToken)).Items).CommentId);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal(addressedId, Assert.Single(comment.Mentions).UserId);
        }

        [Fact]
        public async Task GetMyMentions_ListsMentionsFromEveryProjectTheUserBelongsTo()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-projects");
            var (mentionedId, mentionedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Member, "Cross Project Mention", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Home project mention task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Pinged at home", ownerToken, new[] { mentionedId });

            var otherProjectId = (await _helpers.CreateProjectAsync($"Project {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;
            await _helpers.AddProjectMemberAsync(otherProjectId, mentionedId, ProjectRole.Viewer, ownerToken);

            var otherTaskId = await _helpers.CreateTaskAsync(otherProjectId, "Other project mention task", ownerToken);
            await _helpers.AddTaskCommentAsync(otherTaskId, "Pinged elsewhere", ownerToken, new[] { mentionedId });

            var result = await _helpers.GetMyMentionsResultAsync(mentionedToken);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(new[] { "Home project mention task", "Other project mention task" },
                result.Items.Select(x => x.TaskTitle).OrderBy(x => x).ToArray());
            Assert.Equal(new[] { taskId, otherTaskId }, result.Items.Select(x => x.TaskId).OrderBy(x => x).ToArray());
        }

        [Fact]
        public async Task GetMyMentions_PagesThroughTheFeed()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-paging");
            var (mentionedId, mentionedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Member, "Paged Mention", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "Paged mention task one", ownerToken);
            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Paged mention task two", ownerToken);

            var firstId = await _helpers.AddTaskCommentAsync(firstTaskId, "Ping one", ownerToken, new[] { mentionedId });
            var secondId = await _helpers.AddTaskCommentAsync(secondTaskId, "Ping two", ownerToken, new[] { mentionedId });
            var thirdId = await _helpers.AddTaskCommentAsync(firstTaskId, "Ping three", ownerToken, new[] { mentionedId });

            var firstPage = await _helpers.GetMyMentionsResultAsync(mentionedToken, "page=1&pageSize=2");
            var secondPage = await _helpers.GetMyMentionsResultAsync(mentionedToken, "page=2&pageSize=2");

            Assert.Equal(2, firstPage.Items.Count);
            Assert.Single(secondPage.Items);
            Assert.Equal(3, firstPage.TotalCount);
            Assert.Equal(3, secondPage.TotalCount);

            Assert.Equal(new[] { firstId, secondId, thirdId },
                firstPage.Items.Concat(secondPage.Items).Select(x => x.CommentId).OrderBy(x => x).ToArray());
            Assert.Empty(firstPage.Items.Select(x => x.CommentId).Intersect(secondPage.Items.Select(x => x.CommentId)));
        }

        [Fact]
        public async Task GetMyMentions_AfterAnEditDropsTheMention_ExcludesIt()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-edited");
            var (_, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Editing Author", ownerToken);
            var (droppedId, droppedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Dropped Mention", ownerToken);
            var (keptId, keptToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Kept Mention", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Edited mention task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Pinging both of you", authorToken,
                new[] { droppedId, keptId });

            Assert.NotEmpty((await _helpers.GetMyMentionsResultAsync(droppedToken)).Items);

            await _helpers.EditTaskCommentAsync(taskId, commentId, "Only one of you now", authorToken, new[] { keptId });

            Assert.Empty((await _helpers.GetMyMentionsResultAsync(droppedToken)).Items);

            var kept = Assert.Single((await _helpers.GetMyMentionsResultAsync(keptToken)).Items);

            Assert.Equal(commentId, kept.CommentId);
            Assert.Equal("Only one of you now", kept.Content);
        }

        [Fact]
        public async Task GetMyMentions_AfterTheMentionedUserLeavesTheProject_ExcludesIt()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-left");
            var (_, authorToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Left Project Author", ownerToken);

            var (mentionedId, _, mentionedToken) = await _authenticatedUser.CreateConfirmedUserAsync("cmt-mentions-left-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, mentionedId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var membershipId = await _helpers.AddProjectMemberAsync(projectId, mentionedId, ProjectRole.Viewer, ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Left project mention task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Pinged before leaving", authorToken, new[] { mentionedId });

            Assert.NotEmpty((await _helpers.GetMyMentionsResultAsync(mentionedToken)).Items);

            await _helpers.RemoveProjectMemberAsync(projectId, membershipId, ownerToken);

            Assert.Empty((await _helpers.GetMyMentionsResultAsync(mentionedToken)).Items);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, ownerToken)).Items);

            Assert.Equal(mentionedId, Assert.Single(comment.Mentions).UserId);
        }

        [Fact]
        public async Task GetMyMentions_AfterTheTaskIsDeleted_ExcludesIt()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-deletedtask");
            var (mentionedId, mentionedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Member, "Deleted Task Mention", ownerToken);

            var doomedTaskId = await _helpers.CreateTaskAsync(projectId, "Doomed mention task", ownerToken);
            var keptTaskId = await _helpers.CreateTaskAsync(projectId, "Kept mention task", ownerToken);

            await _helpers.AddTaskCommentAsync(doomedTaskId, "Vanishes with the task", ownerToken, new[] { mentionedId });
            var keptCommentId = await _helpers.AddTaskCommentAsync(keptTaskId, "Stays in the feed", ownerToken, new[] { mentionedId });

            await _helpers.DeleteTaskAsync(doomedTaskId, ownerToken);

            Assert.Equal(keptCommentId, Assert.Single((await _helpers.GetMyMentionsResultAsync(mentionedToken)).Items).CommentId);
        }

        [Fact]
        public async Task GetMyMentions_AfterTheProjectIsDeleted_ExcludesIt()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("cmt-mentions-deletedproject");
            var (mentionedId, mentionedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId,
                ProjectRole.Member, "Deleted Project Mention", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed project mention task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Vanishes with the project", ownerToken, new[] { mentionedId });

            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Cancelled, ownerToken)).StatusCode);

            var otherProjectId = (await _helpers.CreateProjectAsync($"Project {Guid.NewGuid():N}", ownerToken, teamId: teamId)).ProjectId;
            await _helpers.AddProjectMemberAsync(otherProjectId, mentionedId, ProjectRole.Viewer, ownerToken);

            var keptTaskId = await _helpers.CreateTaskAsync(otherProjectId, "Kept project mention task", ownerToken);
            var keptCommentId = await _helpers.AddTaskCommentAsync(keptTaskId, "Stays in the feed", ownerToken, new[] { mentionedId });

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            Assert.Equal(keptCommentId, Assert.Single((await _helpers.GetMyMentionsResultAsync(mentionedToken)).Items).CommentId);
        }

        [Fact]
        public async Task GetMyMentions_WithoutAccessToken_Returns401()
        {
            var response = await _helpers.GetMyMentionsAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


    }
}
