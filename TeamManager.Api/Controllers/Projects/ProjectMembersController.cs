using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember;
using TeamManager.Application.Features.Projects.ProjectMembers.Commands.RemoveMember;

namespace TeamManager.Api.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectMembersController(ISender sender) : ControllerBase
    {
        [HttpPost("projects/{projectId:guid}/members")]
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
    }
}