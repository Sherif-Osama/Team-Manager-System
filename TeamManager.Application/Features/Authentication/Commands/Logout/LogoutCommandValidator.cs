using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.Logout
{
    public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(C => C.RefreshToken)
                .NotEmpty()
                .WithMessage("Refresh token is required.")
                .Must(R => !string.IsNullOrWhiteSpace(R))
                .WithMessage("Refresh token cannot be whitespace.");
        }
    }
}
