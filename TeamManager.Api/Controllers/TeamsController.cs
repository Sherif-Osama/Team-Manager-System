using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Teams.Commands.ActivateTeam;
using TeamManager.Application.Features.Teams.Commands.CreateTeam;
using TeamManager.Application.Features.Teams.Commands.DeactivateTeam;
using TeamManager.Application.Features.Teams.Commands.SoftDeleteTeam;
using TeamManager.Application.Features.Teams.Commands.TransferOwnership;
using TeamManager.Application.Features.Teams.Commands.UpdateTeam;
using TeamManager.Application.Features.Teams.Queries.GetTeam;
using TeamManager.Application.Features.Teams.Queries.GetTeamByName;
using TeamManager.Application.Features.Teams.Queries.GetTeams;

namespace TeamManager.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly ISender _sender;

        public TeamsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CreateTeamCommand command, CancellationToken cancellationToken)
        {
            var teamId = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { teamId }, new { id = teamId });
        }

        [HttpGet("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid teamId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTeamQuery(teamId), cancellationToken);

            return Ok(result);
        }

        [HttpPut("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid teamId, UpdateTeamRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new UpdateTeamCommand(teamId, request.Name, request.Description), cancellationToken);

            return NoContent();
        }

        [HttpPut("{teamId:guid}/owner")]
        [Authorize]
        public async Task<IActionResult> TransferOwnership(Guid teamId, TransferOwnershipRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new TransferOwnershipCommand(teamId, request.NewOwnerUserId), cancellationToken);

            return NoContent();
        }

        [HttpPut("{teamId:guid}/deactivate")]
        [Authorize]
        public async Task<IActionResult> Deactivate(Guid teamId, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeactivateTeamCommand(teamId), cancellationToken);

            return NoContent();
        }

        [HttpPut("{teamId:guid}/activate")]
        [Authorize]
        public async Task<IActionResult> Activate(Guid teamId, CancellationToken cancellationToken)
        {
            await _sender.Send(new ActivateTeamCommand(teamId), cancellationToken);

            return NoContent();
        }

        [HttpDelete("{teamId:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid teamId, CancellationToken cancellationToken)
        {
            await _sender.Send(new SoftDeleteTeamCommand(teamId), cancellationToken);

            return NoContent();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetTeams([FromQuery] GetTeamsQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("by-name")]
        [Authorize]
        public async Task<IActionResult> GetByName([FromQuery] GetTeamByNameQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}