using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskComment.Commands.AddTaskComment;
using TeamManager.Application.Features.Tasks.TaskComment.Commands.DeleteTaskComment;
using TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment;
using TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyComments;
using TeamManager.Application.Features.Tasks.TaskComment.Queries.GetMyMentions;
using TeamManager.Application.Features.Tasks.TaskComment.Queries.GetTaskComments;

namespace TeamManager.Api.Controllers.Tasks
{
    [Route("api/tasks")]
    [ApiController]
    public class TaskCommentsController(ISender sender) : ControllerBase
    {
        [HttpPost("{taskId:long}/comments")]
        [Authorize]
        public async Task<ActionResult<long>> AddTaskComment(long taskId, [FromBody] AddTaskCommentRequest request, CancellationToken cancellationToken)
        {
            var commentId = await sender.Send(new AddTaskCommentCommand(taskId, request.Content, request.MentionedUserIds), cancellationToken);

            return Ok(commentId);
        }

        [HttpPut("{taskId:long}/comments/{commentId:long}")]
        [Authorize]
        public async Task<IActionResult> EditTaskComment(long taskId, long commentId, [FromBody] EditTaskCommentRequest request,
            CancellationToken cancellationToken)
        {
            await sender.Send(new EditTaskCommentCommand(taskId, commentId, request.Content, request.MentionedUserIds), cancellationToken);

            return NoContent();
        }

        [HttpDelete("{taskId:long}/comments/{commentId:long}")]
        [Authorize]
        public async Task<IActionResult> DeleteTaskComment(long taskId, long commentId, CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteTaskCommentCommand(taskId, commentId), cancellationToken);

            return NoContent();
        }

        [HttpGet("{taskId:long}/comments")]
        [Authorize]
        public async Task<ActionResult<GetTaskCommentsResponse>> GetTaskComments(long taskId, [FromQuery] GetTaskCommentsRequest request,
            CancellationToken cancellationToken)
        {
            var response = await sender.Send(new GetTaskCommentsQuery(taskId, request.Page, request.PageSize), cancellationToken);

            return Ok(response);
        }

        [HttpGet("my-comments")]
        [Authorize]
        public async Task<ActionResult<GetMyCommentsResponse>> GetMyComments([FromQuery] GetMyCommentsQuery query,
        CancellationToken cancellationToken)
        {
            var response = await sender.Send(query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("my-mentions")]
        [Authorize]
        public async Task<ActionResult<GetMyMentionsResponse>> GetMyMentions([FromQuery] GetMyMentionsQuery query,
            CancellationToken cancellationToken)
        {
            var response = await sender.Send(query, cancellationToken);

            return Ok(response);
        }
    }
}