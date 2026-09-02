using FluentValidation;

namespace TeamManager.Application.Features.Users.Commands.ChangeEmail
{
    public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
    {
        public ChangeEmailCommandValidator()
        {
            RuleFor(x => x.NewEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.CurrentPassword).NotEmpty().MinimumLength(8);
        }
    }
}
