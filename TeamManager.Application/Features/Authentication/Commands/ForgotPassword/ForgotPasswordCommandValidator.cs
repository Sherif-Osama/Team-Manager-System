using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.ForgotPassword
{
    public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        }
    }
}