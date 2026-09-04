using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Users.SelfManagement.Commands.ChangeEmail;
using TeamManager.Application.Features.Users.SelfManagement.Commands.ChangePassword;
using TeamManager.Application.Features.Users.SelfManagement.Commands.ConfirmEmail;
using TeamManager.Application.Features.Users.SelfManagement.Commands.DeactivateMyAccount;
using TeamManager.Application.Features.Users.SelfManagement.Commands.DeleteMyAccount;
using TeamManager.Application.Features.Users.SelfManagement.Commands.ResendEmailConfirmation;
using TeamManager.Application.Features.Users.SelfManagement.Commands.UpdateMyProfile;
using TeamManager.Application.Features.Users.SelfManagement.Queries.GetMyProfile;

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

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount(DeleteMyAccountCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("me/deactivate")]
        public async Task<IActionResult> DeactivateMyAccount(DeactivateMyAccountCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }
    }
}