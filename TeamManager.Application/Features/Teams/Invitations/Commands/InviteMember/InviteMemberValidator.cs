using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.InviteMember
{
    public sealed class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
    {
        public InviteMemberValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

            RuleFor(x => x.TeamRole).IsInEnum();
        }
    }
}