using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Tasks.TaskDependency.Commands.AddTaskDependency;
using TeamManager.Application.Features.Tasks.TaskDependency.Commands.DeleteTaskDependency;
using TeamManager.Application.Features.Tasks.TaskDependency.Queries.GetTaskDependencies;

namespace TeamManager.Api.Controllers.Tasks
{
    [Route("api/tasks")]
    [ApiController]
    public class TaskDependenciesController(ISender sender) : ControllerBase
    {
        [HttpPost("{taskId:long}/dependencies")]
        [Authorize]
        public async Task<ActionResult<long>> AddTaskDependency(long taskId, [FromBody] AddTaskDependencyRequest request,
         CancellationToken cancellationToken)
        {
            var dependencyId = await sender.Send(new AddTaskDependencyCommand(taskId, request.DependsOnTaskId), cancellationToken);

            return Ok(dependencyId);
        }

        [HttpDelete("{taskId:long}/dependencies/{dependencyId:long}")]
        [Authorize]
        public async Task<IActionResult> DeleteTaskDependency(long taskId, long dependencyId, CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteTaskDependencyCommand(taskId, dependencyId), cancellationToken);

            return NoContent();
        }

        [HttpGet("{taskId:long}/dependencies")]
        [Authorize]
        public async Task<ActionResult<GetTaskDependenciesResponse>> GetTaskDependencies(long taskId,
         [FromQuery] GetTaskDependenciesRequest request, CancellationToken cancellationToken)
        {
            var response = await sender.Send(new GetTaskDependenciesQuery(taskId, request.Page, request.PageSize),
                cancellationToken);

            return Ok(response);
        }
    }
}