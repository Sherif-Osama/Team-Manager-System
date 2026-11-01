using FluentValidation;

namespace TeamManager.Application.Features.Roles.Commands.CreateRole
{
    public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Role name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Role name cannot be whitespace.")
                .MaximumLength(50)
                .WithMessage("Role name must not exceed 50 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(256)
                .WithMessage("Role description must not exceed 256 characters.");
        }
    }
}
