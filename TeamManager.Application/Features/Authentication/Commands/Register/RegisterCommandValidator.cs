using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(C => C.Email).NotEmpty().Must(E => !string.IsNullOrWhiteSpace(E)).MaximumLength(256).EmailAddress();

        RuleFor(x => x.DisplayName).NotEmpty().Must(N => !string.IsNullOrWhiteSpace(N)).MaximumLength(100);

        RuleFor(x => x.Password).NotEmpty().Must(P => !string.IsNullOrWhiteSpace(P)).MinimumLength(8);
    }
}