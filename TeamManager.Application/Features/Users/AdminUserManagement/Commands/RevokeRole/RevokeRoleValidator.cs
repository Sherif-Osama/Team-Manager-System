using FluentValidation;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole
{
    public sealed class RevokeRoleValidator : AbstractValidator<RevokeRoleCommand>
    {
        public RevokeRoleValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RoleId).GreaterThan(0);
        }
    }
}