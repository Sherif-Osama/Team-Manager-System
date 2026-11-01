using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(C => C.RefreshToken)
                .NotEmpty()
                .WithMessage("Refresh token is required.")
                .Must(R => !string.IsNullOrWhiteSpace(R))
                .WithMessage("Refresh token cannot be whitespace.");
        }
    }
}
