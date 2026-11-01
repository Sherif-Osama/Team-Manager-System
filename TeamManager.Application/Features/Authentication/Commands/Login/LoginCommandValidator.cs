using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(C => C.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .Must(E => !string.IsNullOrWhiteSpace(E))
            .WithMessage("Email cannot be whitespace.")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.")
            .EmailAddress()
            .WithMessage("Email format is invalid.");

        RuleFor(C => C.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .Must(P => !string.IsNullOrWhiteSpace(P))
            .WithMessage("Password cannot be whitespace.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }
}
