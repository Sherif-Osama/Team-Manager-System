using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskChecklist.Commands.AddChecklistItem;
using TeamManager.Application.Features.Tasks.TaskChecklist.Commands.CompleteChecklistItem;
using TeamManager.Application.Features.Tasks.TaskChecklist.Commands.RemoveChecklistItem;
using TeamManager.Application.Features.Tasks.TaskChecklist.Commands.ReopenChecklistItem;
using TeamManager.Application.Features.Tasks.TaskChecklist.Queries.GetTaskChecklist;

namespace TeamManager.Api.Controllers.Tasks
{
    [Route("api/tasks")]
    [ApiController]
    public class TaskChecklistController(ISender sender) : ControllerBase
    {
        [HttpPost("{taskId:long}/checklist")]
        [Authorize]
        public async Task<ActionResult<long>> AddChecklistItem(long taskId, [FromBody] AddChecklistItemRequest request,
        CancellationToken cancellationToken)
        {
            var itemId = await sender.Send(new AddChecklistItemCommand(taskId, request.Content), cancellationToken);

            return Ok(itemId);
        }

        [HttpPut("{taskId:long}/checklist/{checklistItemId:long}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteChecklistItem(long taskId, long checklistItemId,
            CancellationToken cancellationToken)
        {
            await sender.Send(new CompleteChecklistItemCommand(taskId, checklistItemId), cancellationToken);

            return NoContent();
        }

        [HttpPut("{taskId:long}/checklist/{checklistItemId:long}/reopen")]
        [Authorize]
        public async Task<IActionResult> ReopenChecklistItem(long taskId, long checklistItemId, CancellationToken cancellationToken)
        {
            await sender.Send(new ReopenChecklistItemCommand(taskId, checklistItemId), cancellationToken);

            return NoContent();
        }

        [HttpDelete("{taskId:long}/checklist/{checklistItemId:long}")]
        [Authorize]
        public async Task<IActionResult> RemoveChecklistItem(long taskId, long checklistItemId, CancellationToken cancellationToken)
        {
            await sender.Send(new RemoveChecklistItemCommand(taskId, checklistItemId), cancellationToken);

            return NoContent();
        }

        [HttpGet("{taskId:long}/checklist")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyCollection<GetTaskChecklistResponse>>> GetTaskChecklist(long taskId,
        [FromQuery] string? search, [FromQuery] bool? isCompleted, CancellationToken cancellationToken)
        {
            var response = await sender.Send(new GetTaskChecklistQuery(taskId, search, isCompleted), cancellationToken);

            return Ok(response);
        }
    }
}