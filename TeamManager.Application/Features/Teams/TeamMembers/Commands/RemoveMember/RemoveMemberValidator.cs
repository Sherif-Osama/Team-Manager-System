using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.RemoveMember
{
    public sealed class RemoveMemberValidator : AbstractValidator<RemoveMemberCommand>
    {
        public RemoveMemberValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.MemberId)
                .GreaterThan(0)
                .WithMessage("Member ID must be greater than 0.");
        }
    }
}
