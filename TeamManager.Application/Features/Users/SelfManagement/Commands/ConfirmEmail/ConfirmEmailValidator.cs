using FluentValidation;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.ConfirmEmail
{
    public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailValidator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
        }
    }
}