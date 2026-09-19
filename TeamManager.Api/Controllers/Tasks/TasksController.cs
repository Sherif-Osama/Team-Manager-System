using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask;
using TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTaskById;

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

            return CreatedAtAction(nameof(GetById), new { taskId }, taskId);
        }

        [HttpGet("{taskId:long}")]
        [Authorize]
        public async Task<ActionResult<GetTaskByIdResponse>> GetById(long taskId, CancellationToken cancellationToken)
        {
            var response = await sender.Send(new GetTaskByIdQuery(taskId), cancellationToken);

            return Ok(response);
        }
    }
}