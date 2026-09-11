using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Projects.Project.Commands.ChangeProjectStatus;
using TeamManager.Application.Features.Projects.Project.Commands.CreateProject;
using TeamManager.Application.Features.Projects.Project.Commands.ScheduleProject;
using TeamManager.Application.Features.Projects.Project.Commands.UpdateProject;
using TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember;

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

            //return CreatedAtAction(nameof(GetById), new { id = projectId }, projectId);
            return Ok();
        }

        [HttpPut("projects/{projectId:guid}/schedule")]
        [Authorize]
        public async Task<IActionResult> ScheduleProject(Guid projectId, [FromBody] ScheduleProjectRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ScheduleProjectCommand(projectId, request.StartDate, request.DueDate);

            await sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpPut("projects/{projectId:guid}")]
        public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectRequest request,
            CancellationToken cancellationToken)
        {
            await sender.Send(new UpdateProjectCommand(projectId, request.Name, request.Description), cancellationToken);

            return NoContent();
        }

        [HttpPut("projects/{projectId:guid}/status")]
        public async Task<IActionResult> ChangeProjectStatus(Guid projectId, [FromBody] ChangeProjectStatusRequest request,
        CancellationToken cancellationToken)
        {
            await sender.Send(new ChangeProjectStatusCommand(projectId, request.Status), cancellationToken);

            return NoContent();
        }

        [HttpPost("projects/{projectId:guid}/members")]
        public async Task<IActionResult> AddProjectMember(Guid projectId, [FromBody] AddProjectMemberRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new AddProjectMemberCommand(projectId, request.UserId, request.ProjectRole), cancellationToken);

            return NoContent();
        }
    }
}