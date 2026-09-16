using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember;
using TeamManager.Application.Features.Projects.ProjectMembers.Commands.ChangeMemberRole;
using TeamManager.Application.Features.Projects.ProjectMembers.Commands.RemoveMember;
using TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetMyProjects;
using TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetProjectMembers;
using TeamManager.Domain.Enums;

namespace TeamManager.Api.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectMembersController(ISender sender) : ControllerBase
    {
        [HttpPost("{projectId:guid}/members")]
        [Authorize]
        public async Task<IActionResult> AddProjectMember(Guid projectId, [FromBody] AddProjectMemberRequest request,
            CancellationToken cancellationToken)
        {
            await sender.Send(new AddProjectMemberCommand(projectId, request.UserId, request.ProjectRole), cancellationToken);

            return NoContent();
        }

        [HttpDelete("{projectId:guid}/members{memberId:long}")]
        [Authorize]
        public async Task<IActionResult> RemoveProjectMember(Guid projectId, long memberId, CancellationToken cancellationToken)
        {
            await sender.Send(new RemoveMemberCommand(projectId, memberId), cancellationToken);

            return NoContent();
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyProjects([FromQuery] GetMyProjectsQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("{projectId:guid}/members")]
        [Authorize]
        public async Task<IActionResult> GetProjectMembers(Guid projectId, [FromQuery] GetProjectMembersQuery query, CancellationToken cancellationToken)
        {
            var request = query with { ProjectId = projectId };

            var result = await sender.Send(request, cancellationToken);

            return Ok(result);
        }

        [HttpPut("{projectId:guid}/members/{projectMemberId:long}/role")]
        [Authorize]
        public async Task<IActionResult> ChangeMemberRole(Guid projectId, long projectMemberId, [FromBody] ProjectRole role, CancellationToken cancellationToken)
        {
            await sender.Send(new ChangeMemberRoleCommand(projectId, projectMemberId, role), cancellationToken);

            return NoContent();
        }
    }
}