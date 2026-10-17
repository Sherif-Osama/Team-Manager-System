using FluentValidation;

namespace TeamManager.Application.Features.Roles.Commands.CreateRole
{
    public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Role name is required.").MaximumLength(50);

            RuleFor(x => x.Description).MaximumLength(256);
        }
    }
}