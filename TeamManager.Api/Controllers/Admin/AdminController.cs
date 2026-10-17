using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Application.Features.Admin.Commands.BootstrapAdmin;
using TeamManager.Application.Features.AuditLogs.Queries;
using TeamManager.Application.Features.Permissions.Queries.GetPermissions;
using TeamManager.Application.Features.Roles.Queries.GetRoles;
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
        public async Task<IActionResult> Bootstrap(BootstrapAdminCommand command, CancellationToken cancellationToken)
        {
            await sender.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("users/{userId:guid}/roles")]
        [Authorize(Roles = DefaultRoles.Admin)]
        public async Task<IActionResult> AssignRole(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken)
        {
            await sender.Send(new AssignRoleCommand(userId, request.RoleId), cancellationToken);

            return NoContent();
        }

        [HttpGet("users")]
        [Authorize(Roles = DefaultRoles.Admin)]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("users/{userId:guid}")]
        [Authorize(Roles = DefaultRoles.Admin)]
        public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("users/by-email/{email}")]
        [Authorize(Roles = DefaultRoles.Admin)]
        public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetUserByEmailQuery(email), cancellationToken);
            return Ok(result);
        }

        [HttpDelete("users/{userId:guid}/roles/{roleId:int}")]
        [Authorize(Roles = DefaultRoles.Admin)]
        public async Task<IActionResult> RevokeRole(Guid userId, int roleId, CancellationToken cancellationToken)
        {
            await sender.Send(new RevokeRoleCommand(userId, roleId), cancellationToken);
            return NoContent();
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] GetAuditLogsQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetPermissionsQuery(), cancellationToken);

            return Ok(result);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetRolesQuery(), cancellationToken);

            return Ok(result);
        }
    }
}