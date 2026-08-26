using MediatR;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Authentication.Commands.Register;

namespace TeamManager.Api.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public sealed class AuthenticationController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthenticationController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
        {
            var userId = await _sender.Send(command, cancellationToken);
            return Ok(userId);
        }
    }
}