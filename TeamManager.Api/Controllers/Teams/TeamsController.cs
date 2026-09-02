using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Teams.Team.Commands.ActivateTeam;
using TeamManager.Application.Features.Teams.Team.Commands.CreateTeam;
using TeamManager.Application.Features.Teams.Team.Commands.DeactivateTeam;
using TeamManager.Application.Features.Teams.Team.Commands.SoftDeleteTeam;
using TeamManager.Application.Features.Teams.Team.Commands.TransferOwnership;
using TeamManager.Application.Features.Teams.Team.Commands.UpdateTeam;
using TeamManager.Application.Features.Teams.Team.Queries.GetTeam;
using TeamManager.Application.Features.Teams.Team.Queries.GetTeamByName;
using TeamManager.Application.Features.Teams.Team.Queries.GetTeams;

namespace TeamManager.Api.Controllers.Teams
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController(ISender sender) : ControllerBase
    {
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CreateTeamCommand command, CancellationToken cancellationToken)
        {
            var teamId = await sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { teamId }, new { id = teamId });
        }

        [HttpGet("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid teamId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetTeamQuery(teamId), cancellationToken);

            return Ok(result);
        }

        [HttpPut("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid teamId, UpdateTeamRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new UpdateTeamCommand(teamId, request.Name, request.Description), cancellationToken);

            return NoContent();
        }

        [HttpPut("{teamId:guid}/owner")]
        [Authorize]
        public async Task<IActionResult> TransferOwnership(Guid teamId, TransferOwnershipRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new TransferOwnershipCommand(teamId, request.NewOwnerUserId), cancellationToken);

            return NoContent();
        }

        [HttpPut("{teamId:guid}/deactivate")]
        [Authorize]
        public async Task<IActionResult> Deactivate(Guid teamId, CancellationToken cancellationToken)
        {
            await sender.Send(new DeactivateTeamCommand(teamId), cancellationToken);

            return NoContent();
        }

        [HttpPut("{teamId:guid}/activate")]
        [Authorize]
        public async Task<IActionResult> Activate(Guid teamId, CancellationToken cancellationToken)
        {
            await sender.Send(new ActivateTeamCommand(teamId), cancellationToken);

            return NoContent();
        }

        [HttpDelete("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid teamId, CancellationToken cancellationToken)
        {
            await sender.Send(new SoftDeleteTeamCommand(teamId), cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTeams([FromQuery] GetTeamsQuery request, CancellationToken cancellationToken)
        {


            var result = await sender.Send(request, cancellationToken);

            return Ok(result);
        }

        [HttpGet("by-name")]
        [Authorize]
        public async Task<IActionResult> GetByName([FromQuery] GetTeamByNameQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}