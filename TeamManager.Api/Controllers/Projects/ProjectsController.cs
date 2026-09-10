using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Projects.Project.Commands.CreateProject;

namespace TeamManager.Api.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController(ISender sender) : ControllerBase
    {
        [HttpPost("/api/teams/{teamId:guid}/projects")]
        [Authorize]
        public async Task<IActionResult> CreateProject(Guid teamId, CreateProjectRequest request,
            CancellationToken cancellationToken)
        {
            var projectId = await sender.Send(new CreateProjectCommand(teamId, request.Name, request.Description),
                cancellationToken);

            //return CreatedAtAction(nameof(GetById), new { id = projectId }, projectId);
            return Ok();
        }
    }
}