using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Teams.TeamMembers.Commands.AddMember;
using TeamManager.Application.Features.Teams.TeamMembers.Commands.ChangeMemberRole;
using TeamManager.Application.Features.Teams.TeamMembers.Commands.RemoveMember;
using TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember;
using TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers;

namespace TeamManager.Api.Controllers.Teams
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamMembersController(ISender sender) : ControllerBase
    {
        [HttpPost("{teamId:guid}/member")]
        [Authorize]
        public async Task<IActionResult> AddMember(Guid teamId, AddMemberRequest request, CancellationToken cancellationToken)
        {
            var memberId = await sender.Send(new AddMemberCommand(teamId, request.UserId, request.TeamRole), cancellationToken);

            return CreatedAtAction(nameof(GetMember), new { teamId, memberId }, new { id = memberId });
        }

        [HttpGet("{teamId:guid}/members")]
        [Authorize]
        public async Task<IActionResult> GetMembers(Guid teamId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new GetMembersQuery(teamId, page, pageSize), cancellationToken);
            return Ok(result);
        }


        [HttpGet("{teamId:guid}/members/{memberId:long}")]
        [Authorize]
        public async Task<IActionResult> GetMember(Guid teamId, long memberId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetMemberQuery(teamId, memberId), cancellationToken);

            return Ok(result);
        }

        [HttpDelete("{teamId:guid}/members/{memberId:long}")]
        [Authorize]
        public async Task<IActionResult> RemoveMember(Guid teamId, long memberId, CancellationToken cancellationToken)
        {
            await sender.Send(new RemoveMemberCommand(teamId, memberId), cancellationToken);

            return NoContent();
        }


        [HttpPut("{teamId:guid}/members/{memberId:long}/role")]
        [Authorize]
        public async Task<IActionResult> ChangeMemberRole(Guid teamId, long memberId, ChangeMemberRoleRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new ChangeMemberRoleCommand(teamId, memberId, request.Role), cancellationToken);

            return NoContent();
        }
    }
}
