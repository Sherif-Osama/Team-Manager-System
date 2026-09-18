using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Projects.Project.Commands.ChangeProjectStatus;
using TeamManager.Application.Features.Projects.Project.Commands.CreateProject;
using TeamManager.Application.Features.Projects.Project.Commands.ScheduleProject;
using TeamManager.Application.Features.Projects.Project.Commands.TransferProjectOwnership;
using TeamManager.Application.Features.Projects.Project.Commands.UpdateProject;
using TeamManager.Application.Features.Projects.Project.Queries.GetProject;
using TeamManager.Application.Features.Projects.Project.Queries.GetProjectByName;
using TeamManager.Application.Features.Projects.Project.Queries.GetProjects;

namespace TeamManager.Api.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController(ISender sender) : ControllerBase
    {
        [HttpPost("/api/teams/{teamId:guid}/projects")]
        [Authorize]
        public async Task<IActionResult> CreateProject(Guid teamId, CreateProjectRequest request, CancellationToken cancellationToken)
        {
            var projectId = await sender.Send(new CreateProjectCommand(teamId, request.Name, request.Description, request.startDate, request.dueDate),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { projectId }, projectId);
        }

        [HttpPut("{projectId:guid}/schedule")]
        [Authorize]
        public async Task<IActionResult> ScheduleProject(Guid projectId, [FromBody] ScheduleProjectRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ScheduleProjectCommand(projectId, request.StartDate, request.DueDate);

            await sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpPut("{projectId:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectRequest request,
            CancellationToken cancellationToken)
        {
            await sender.Send(new UpdateProjectCommand(projectId, request.Name, request.Description), cancellationToken);

            return NoContent();
        }

        [HttpPut("{projectId:guid}/status")]
        [Authorize]
        public async Task<IActionResult> ChangeProjectStatus(Guid projectId, [FromBody] ChangeProjectStatusRequest request,
        CancellationToken cancellationToken)
        {
            await sender.Send(new ChangeProjectStatusCommand(projectId, request.Status), cancellationToken);

            return NoContent();
        }

        [HttpPut("{projectId:guid}/owner")]
        [Authorize]
        public async Task<IActionResult> TransferOwnership(Guid projectId, TransferProjectOwnershipRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new TransferProjectOwnershipCommand(projectId, request.NewOwnerUserId), cancellationToken);

            return NoContent();
        }

        [HttpGet("{projectId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid projectId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetProjectQuery(projectId), cancellationToken);

            return Ok(result);
        }

        [HttpGet("by-name")]
        [Authorize]
        public async Task<IActionResult> GetByName([FromQuery] string name, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetProjectByNameQuery(name), cancellationToken);

            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetProjects([FromQuery] GetProjectsQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}