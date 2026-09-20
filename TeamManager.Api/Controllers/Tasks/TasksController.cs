using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskStatus;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.RescheduleTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.UpdateTask;
using TeamManager.Application.Features.Tasks.TaskItem.Queries.GetMyTasks;
using TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTask;

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

        [HttpPut("{taskId:long}")]
        [Authorize]
        public async Task<IActionResult> UpdateTask(long taskId, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new UpdateTaskCommand(taskId, request.Title, request.Description), cancellationToken);

            return NoContent();
        }

        [HttpPut("{taskId:long}/status")]
        [Authorize]
        public async Task<IActionResult> ChangeTaskStatus(long taskId, [FromBody] ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new ChangeTaskStatusCommand(taskId, request.Status), cancellationToken);

            return NoContent();
        }

        [HttpPut("{taskId:long}/schedule")]
        [Authorize]
        public async Task<IActionResult> RescheduleTask(long taskId, [FromBody] RescheduleTaskRequest request,
            CancellationToken cancellationToken)
        {
            await sender.Send(new RescheduleTaskCommand(taskId, request.StartDate, request.DueDate), cancellationToken);

            return NoContent();
        }

        [HttpGet("{taskId:long}")]
        [Authorize]
        public async Task<ActionResult<GetTaskResponse>> GetById(long taskId, CancellationToken cancellationToken)
        {
            var response = await sender.Send(new GetTaskQuery(taskId), cancellationToken);

            return Ok(response);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<GetMyTasksResponse>> GetMyTasks([FromQuery] GetMyTasksQuery query, CancellationToken cancellationToken)
        {
            var response = await sender.Send(query, cancellationToken);

            return Ok(response);
        }
    }
}