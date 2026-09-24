using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.AssignTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskPriority;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskStatus;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.DeleteTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.RescheduleTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.TaskDependency;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.UnassignTask;
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

        [HttpPut("{taskId:long}/priority")]
        [Authorize]
        public async Task<IActionResult> ChangeTaskPriority(long taskId, [FromBody] ChangeTaskPriorityRequest request,
            CancellationToken cancellationToken)
        {
            await sender.Send(new ChangeTaskPriorityCommand(taskId, request.Priority), cancellationToken);

            return NoContent();
        }

        [HttpPost("{taskId:long}/dependencies")]
        [Authorize]
        public async Task<ActionResult<long>> AddTaskDependency(long taskId, [FromBody] AddTaskDependencyRequest request,
            CancellationToken cancellationToken)
        {
            var dependencyId = await sender.Send(new AddTaskDependencyCommand(taskId, request.DependsOnTaskId), cancellationToken);

            return Ok(dependencyId);
        }

        [HttpPut("{taskId:long}/assignee")]
        [Authorize]
        public async Task<IActionResult> AssignTask(long taskId, [FromBody] AssignTaskRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new AssignTaskCommand(taskId, request.UserId), cancellationToken);

            return NoContent();
        }

        [HttpDelete("{taskId:long}")]
        [Authorize]
        public async Task<IActionResult> DeleteTask(long taskId, CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteTaskCommand(taskId), cancellationToken);

            return NoContent();

        }

        [HttpDelete("{taskId:long}/assignee")]
        [Authorize]
        public async Task<IActionResult> UnassignTask(long taskId, CancellationToken cancellationToken)
        {
            await sender.Send(new UnassignTaskCommand(taskId), cancellationToken);

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