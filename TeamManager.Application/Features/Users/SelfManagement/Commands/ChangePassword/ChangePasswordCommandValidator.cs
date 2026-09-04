using FluentValidation;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.ChangePassword
{
    public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();

            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).NotEqual(x => x.CurrentPassword);

            RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword);
        }
    }
}