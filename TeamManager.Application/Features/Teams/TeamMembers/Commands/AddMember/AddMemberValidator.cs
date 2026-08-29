using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Commands.AddMember
{
    public sealed class AddMemberValidator : AbstractValidator<AddMemberCommand>
    {
        public AddMemberValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.UserId).NotEmpty();

            RuleFor(x => x.TeamRole).IsInEnum();
        }
    }
}
