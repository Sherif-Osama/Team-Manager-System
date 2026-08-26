using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();

        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}