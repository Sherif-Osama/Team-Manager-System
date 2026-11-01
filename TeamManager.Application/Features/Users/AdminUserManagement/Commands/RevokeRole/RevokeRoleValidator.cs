using FluentValidation;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole
{
    public sealed class RevokeRoleValidator : AbstractValidator<RevokeRoleCommand>
    {
        public RevokeRoleValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("Role ID must be greater than 0.");
        }
    }
}
