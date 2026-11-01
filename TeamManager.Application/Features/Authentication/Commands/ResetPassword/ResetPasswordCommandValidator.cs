using FluentValidation;

namespace TeamManager.Application.Features.Authentication.Commands.ResetPassword
{
    public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .MaximumLength(256)
                .WithMessage("Email must not exceed 256 characters.")
                .EmailAddress()
                .WithMessage("Email format is invalid.");

            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Reset token is required.")
                .MaximumLength(128)
                .WithMessage("Reset token must not exceed 128 characters.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required.")
                .MinimumLength(8)
                .WithMessage("New password must be at least 8 characters.");

            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword)
                .WithMessage("Passwords do not match.");
        }
    }
}
