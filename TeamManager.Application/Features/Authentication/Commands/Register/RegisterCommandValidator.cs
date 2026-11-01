using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
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

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required.")
            .Must(N => !string.IsNullOrWhiteSpace(N))
            .WithMessage("Display name cannot be whitespace.")
            .MaximumLength(100)
            .WithMessage("Display name must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .Must(P => !string.IsNullOrWhiteSpace(P))
            .WithMessage("Password cannot be whitespace.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }
}
