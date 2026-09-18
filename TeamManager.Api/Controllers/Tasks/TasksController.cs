using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask;

namespace TeamManager.Api.Controllers.Tasks
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController(ISender sender) : ControllerBase
    {
        [HttpPost("{projectId:guid}/tasks")]
        [Authorize]
        public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskRequest request,
            CancellationToken cancellationToken)
        {
            var taskId = await sender.Send(new CreateTaskCommand(projectId, request.Title, request.Description,
                request.AssigneeUserId, request.StartDate, request.DueDate, request.Priority), cancellationToken);

            //return CreatedAtAction(nameof(GetTaskById), new { taskId }, taskId);

            return Ok(taskId);
        }
    }
}