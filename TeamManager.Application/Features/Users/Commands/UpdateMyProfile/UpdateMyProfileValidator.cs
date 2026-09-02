using FluentValidation;

namespace TeamManager.Application.Features.Users.Commands.UpdateMyProfile
{
    public sealed class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
    {
        public UpdateMyProfileValidator()
        {
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        }
    }
}