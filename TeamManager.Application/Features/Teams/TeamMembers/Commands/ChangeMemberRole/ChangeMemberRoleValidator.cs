using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleValidator : AbstractValidator<ChangeMemberRoleCommand>
    {
        public ChangeMemberRoleValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty();

            RuleFor(x => x.MemberId).GreaterThan(0);

            RuleFor(x => x.Role).IsInEnum();
        }
    }
}