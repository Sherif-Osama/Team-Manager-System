using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleValidator : AbstractValidator<ChangeMemberRoleCommand>
    {
        public ChangeMemberRoleValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.MemberId)
                .GreaterThan(0)
                .WithMessage("Member ID must be greater than 0.");

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid team role.");
        }
    }
}
