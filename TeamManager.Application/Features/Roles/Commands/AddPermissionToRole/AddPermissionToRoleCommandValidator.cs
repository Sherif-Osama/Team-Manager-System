using FluentValidation;

namespace TeamManager.Application.Features.Roles.Commands.AddPermissionToRole
{
    public sealed class AddPermissionToRoleCommandValidator : AbstractValidator<AddPermissionToRoleCommand>
    {
        public AddPermissionToRoleCommandValidator()
        {
            RuleFor(x => x.RoleId).GreaterThan(0);
            RuleFor(x => x.PermissionId).GreaterThan(0);
        }
    }
}
