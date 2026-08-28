using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(C => C.RefreshToken).NotEmpty().Must(R => !string.IsNullOrWhiteSpace(R));
        }
    }
}
