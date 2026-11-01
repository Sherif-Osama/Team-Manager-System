using FluentValidation;

namespace TeamManager.Application.Features.Roles.Commands.RenameRole
{
    public sealed class RenameRoleCommandValidator : AbstractValidator<RenameRoleCommand>
    {
        public RenameRoleCommandValidator()
        {
            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .WithMessage("Role ID must be greater than 0.");

            RuleFor(x => x.NewName)
                .NotEmpty()
                .WithMessage("Role name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Role name cannot be whitespace.")
                .MaximumLength(50)
                .WithMessage("Role name must not exceed 50 characters.");
        }
    }
}
