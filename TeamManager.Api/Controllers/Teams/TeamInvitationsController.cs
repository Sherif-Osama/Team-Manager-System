using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Teams.Invitations.Commands.AcceptInvitation;
using TeamManager.Application.Features.Teams.Invitations.Commands.CancelInvitation;
using TeamManager.Application.Features.Teams.Invitations.Commands.InviteMember;
using TeamManager.Application.Features.Teams.Invitations.Commands.RejectInvitation;
using TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations;
using TeamManager.Application.Features.Teams.Invitations.Queries.GetMyInvitations;
using TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations;

namespace TeamManager.Api.Controllers.Teams
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamInvitationsController(ISender sender) : ControllerBase
    {
        [HttpPost("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> InviteMember(Guid teamId, InviteMemberRequest request, CancellationToken cancellationToken)
        {
            var invitationId = await sender.Send(new InviteMemberCommand(teamId, request.Email, request.TeamRole), cancellationToken);

            return Created(string.Empty, new { id = invitationId });
        }

        [HttpPost("accept")]
        [Authorize]
        public async Task<IActionResult> AcceptInvitation(AcceptInvitationCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpPost("reject")]
        [Authorize]
        public async Task<IActionResult> RejectInvitation(RejectInvitationCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpDelete("{teamId:guid}/{invitationId:guid}")]
        [Authorize]
        public async Task<IActionResult> CancelInvitation(Guid teamId, Guid invitationId, CancellationToken cancellationToken)
        {
            await sender.Send(new CancelInvitationCommand(teamId, invitationId), cancellationToken);

            return NoContent();
        }

        [HttpGet("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetInvitations(Guid teamId, [FromQuery] GetInvitationsRequest request, CancellationToken cancellationToken)
        {
            var query = new GetInvitationsQuery(teamId, request.Search, request.Status,
                request.Role, request.Page, request.PageSize);

            var result = await sender.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("by-email")]
        [Authorize]
        public async Task<IActionResult> GetInvitationsByEmail([FromQuery] GetInvitationsByEmailRequest request, CancellationToken cancellationToken)
        {
            var query = new GetInvitationsByEmailQuery(request.TeamId, request.Email, request.Status,
                request.Page, request.PageSize);

            var result = await sender.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyInvitations([FromQuery] GetMyInvitationsQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}