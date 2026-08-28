using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(C => C.Email).NotEmpty().Must(E => !string.IsNullOrWhiteSpace(E)).MaximumLength(256).EmailAddress();
        RuleFor(C => C.Password).NotEmpty().Must(P => !string.IsNullOrWhiteSpace(P)).MinimumLength(8);
    }
}