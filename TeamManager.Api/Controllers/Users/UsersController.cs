using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Users.Commands.ChangeEmail;
using TeamManager.Application.Features.Users.Commands.ChangePassword;
using TeamManager.Application.Features.Users.Commands.ConfirmEmail;
using TeamManager.Application.Features.Users.Commands.DeactivateMyAccount;
using TeamManager.Application.Features.Users.Commands.DeleteMyAccount;
using TeamManager.Application.Features.Users.Commands.ResendEmailConfirmation;
using TeamManager.Application.Features.Users.Commands.UpdateMyProfile;
using TeamManager.Application.Features.Users.Queries.GetMyProfile;
using TeamManager.Application.Features.Users.Queries.GetUserByEmail;
using TeamManager.Application.Features.Users.Queries.GetUserById;
using TeamManager.Application.Features.Users.Queries.GetUsers;

namespace TeamManager.Api.Controllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController(ISender sender) : ControllerBase
    {
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetMyProfileQuery(), cancellationToken);

            return Ok(result);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateMyProfileCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPut("me/email")]
        public async Task<IActionResult> ChangeEmail(ChangeEmailCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("me/email/confirm")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("me/email/confirm/resend")]
        public async Task<IActionResult> ResendEmailConfirmation(CancellationToken cancellationToken)
        {
            await sender.Send(new ResendEmailConfirmationCommand(), cancellationToken);
            return NoContent();
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount(CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteMyAccountCommand(), cancellationToken);
            return NoContent();
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUserByEmailQuery(email), cancellationToken);
            return Ok(result);
        }

        [HttpPost("me/deactivate")]
        public async Task<IActionResult> DeactivateMyAccount(CancellationToken cancellationToken)
        {
            await sender.Send(new DeactivateMyAccountCommand(), cancellationToken);
            return NoContent();
        }
    }
}