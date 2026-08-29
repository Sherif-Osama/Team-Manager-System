using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.RemoveMember
{
    public sealed class RemoveMemberValidator : AbstractValidator<RemoveMemberCommand>
    {
        public RemoveMemberValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.MemberId).GreaterThan(0);
        }
    }
}