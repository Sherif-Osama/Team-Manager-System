using FluentValidation;

namespace TeamManager.Application.Features.Roles.Commands.RenameRole
{
    public sealed class RenameRoleCommandValidator : AbstractValidator<RenameRoleCommand>
    {
        public RenameRoleCommandValidator()
        {
            RuleFor(x => x.RoleId).GreaterThan(0);

            RuleFor(x => x.NewName).NotEmpty().Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Role name is required.").MaximumLength(50);
        }
    }
}
