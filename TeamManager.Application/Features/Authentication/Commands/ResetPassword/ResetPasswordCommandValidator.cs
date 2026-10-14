using FluentValidation;
namespace TeamManager.Application.Features.Authentication.Commands.ResetPassword
{
    public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();

            RuleFor(x => x.Token).NotEmpty().MaximumLength(128);

            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);

            RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword);
        }
    }
}