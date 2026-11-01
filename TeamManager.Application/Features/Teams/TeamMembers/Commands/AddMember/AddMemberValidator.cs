using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.AddMember
{
    public sealed class AddMemberValidator : AbstractValidator<AddMemberCommand>
    {
        public AddMemberValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.TeamRole)
                .IsInEnum()
                .WithMessage("Invalid team role.");
        }
    }
}
