using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Features.Admin.Commands.BootstrapAdmin;
using TeamManager.Application.Features.Users.AdminUserManagement.Commands.AssignRole;
using TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole;
using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserByEmail;
using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById;
using TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUsers;

namespace TeamManager.Api.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public sealed class AdminController(ISender sender) : ControllerBase
    {
        [HttpPost("bootstrap")]
        [AllowAnonymous]
        public async Task<IActionResult> Bootstrap(BootstrapAdminCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("users/{userId:guid}/roles")]
        public async Task<IActionResult> AssignRole(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new AssignRoleCommand(userId, request.RoleId), cancellationToken);

            return NoContent();
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("users/{userId:guid}")]
        public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("users/by-email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUserByEmailQuery(email), cancellationToken);
            return Ok(result);
        }

        [HttpDelete("users/{userId:guid}/roles/{roleId:int}")]
        public async Task<IActionResult> RevokeRole(Guid userId, int roleId, CancellationToken cancellationToken)
        {
            await sender.Send(new RevokeRoleCommand(userId, roleId), cancellationToken);
            return NoContent();
        }
    }
}
