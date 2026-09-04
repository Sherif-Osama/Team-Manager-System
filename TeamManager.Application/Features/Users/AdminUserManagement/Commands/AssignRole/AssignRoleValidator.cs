using FluentValidation;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.AssignRole
{
    public sealed class AssignRoleValidator : AbstractValidator<AssignRoleCommand>
    {
        public AssignRoleValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RoleId).GreaterThan(0);
        }
    }
}