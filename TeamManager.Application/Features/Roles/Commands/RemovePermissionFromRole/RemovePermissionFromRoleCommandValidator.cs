using FluentValidation;

namespace TeamManager.Application.Features.Roles.Commands.RemovePermissionFromRole
{
    public sealed class RemovePermissionFromRoleCommandValidator : AbstractValidator<RemovePermissionFromRoleCommand>
    {
        public RemovePermissionFromRoleCommandValidator()
        {
            RuleFor(x => x.RoleId).GreaterThan(0);
            RuleFor(x => x.PermissionId).GreaterThan(0);
        }
    }
}
