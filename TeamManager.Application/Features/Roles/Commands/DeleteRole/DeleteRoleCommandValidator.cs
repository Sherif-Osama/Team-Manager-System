using FluentValidation;

namespace TeamManager.Application.Features.Roles.Commands.DeleteRole
{
    public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
    {
        public DeleteRoleCommandValidator()
        {
            RuleFor(x => x.RoleId).GreaterThan(0);
        }
    }
}
