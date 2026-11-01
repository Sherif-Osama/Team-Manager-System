using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.ForgotPassword
{
    public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .MaximumLength(256)
                .WithMessage("Email must not exceed 256 characters.")
                .EmailAddress()
                .WithMessage("Email format is invalid.");
        }
    }
}
