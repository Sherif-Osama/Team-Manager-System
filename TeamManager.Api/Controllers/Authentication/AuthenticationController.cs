using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamManager.Application.Features.Authentication.Commands.Login;
using TeamManager.Application.Features.Authentication.Commands.Logout;
using TeamManager.Application.Features.Authentication.Commands.RefreshToken;
using TeamManager.Application.Features.Authentication.Commands.Register;

namespace TeamManager.Api.Controllers.Authentication
{

    [ApiController]
    [Route("api/auth")]
    public sealed class AuthenticationController(ISender sender) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
        {
            var userId = await sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(Register), new { id = userId }, new { id = userId });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                UserId = User.FindFirst("sub")?.Value,
                Email = User.FindFirst("email")?.Value,
                Name = User.FindFirst(ClaimTypes.Name)?.Value
            });
        }
    }
}