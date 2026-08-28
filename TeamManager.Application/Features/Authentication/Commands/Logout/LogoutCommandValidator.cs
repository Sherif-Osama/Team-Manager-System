using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.Logout
{
    public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(C => C.RefreshToken).NotEmpty().Must(R => !string.IsNullOrWhiteSpace(R));
        }
    }
}
