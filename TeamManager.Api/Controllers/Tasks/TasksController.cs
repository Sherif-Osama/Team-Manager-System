using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.AssignTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskPriority;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskStatus;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.DeleteTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.RescheduleTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.UnassignTask;
using TeamManager.Application.Features.Tasks.TaskItem.Commands.UpdateTask;
using TeamManager.Application.Features.Tasks.TaskItem.Queries.GetMyTasks;
using TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTask;
using TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTeamTasks;
using TeamManager.Application.Features.Tasks.TaskLabel.Command.RemoveTaskLabel;
using TeamManager.Application.Features.Tasks.TaskLabel.Commands.AddTaskLabel;

namespace TeamManager.Api.Controllers.Tasks
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController(ISender sender) : ControllerBase
    {
        [HttpPost("/api/projects/{projectId:guid}/tasks")]
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

        [HttpPost("{taskId:long}/labels/{labelId:long}")]
        [Authorize]
        public async Task<IActionResult> AddTaskLabel(long taskId, long labelId, CancellationToken cancellationToken)
        {
            await sender.Send(new AddTaskLabelCommand(taskId, labelId), cancellationToken);

            return NoContent();
        }

        [HttpDelete("{taskId:long}/labels/{labelId:long}")]
        [Authorize]
        public async Task<IActionResult> RemoveTaskLabel(long taskId, long labelId, CancellationToken cancellationToken)
        {
            await sender.Send(new RemoveTaskLabelCommand(taskId, labelId), cancellationToken);

            return NoContent();
        }

        [HttpGet("/api/teams/{teamId:guid}/tasks")]
        [Authorize]
        public async Task<ActionResult<GetTeamTasksResponse>> GetTeamTasks(Guid teamId, [FromQuery] GetTeamTasksRequest request, CancellationToken cancellationToken)
        {

            var response = await sender.Send(new GetTeamTasksQuery(teamId, request.Search,
                request.ProjectId, request.Status, request.Priority, request.Page, request.PageSize), cancellationToken);

            return Ok(response);
        }
    }
}