using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using TeamManager.Application.Features.Authentication.Commands.ForgotPassword;
using TeamManager.Application.Features.Authentication.Commands.Login;
using TeamManager.Application.Features.Authentication.Commands.Logout;
using TeamManager.Application.Features.Authentication.Commands.RefreshToken;
using TeamManager.Application.Features.Authentication.Commands.Register;
using TeamManager.Application.Features.Authentication.Commands.ResetPassword;

namespace TeamManager.Api.Controllers.Authentication
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class AuthenticationController(ISender sender) : ControllerBase
    {
        [HttpPost("register")]
        [EnableRateLimiting("AuthRegister")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
        {
            var userId = await sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(Register), new { id = userId }, new { id = userId });
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthLogin")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);

            return Ok(result);
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("AuthRefresh")]
        [AllowAnonymous]
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
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Name = User.FindFirst(ClaimTypes.Name)?.Value
            });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);

            return Ok(new
            {
                message = "If an account with this email exists, password reset instructions will be sent."
            });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);

            return NoContent();
        }
    }
}