using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.CancelInvitation
{
    public sealed class CancelInvitationValidator : AbstractValidator<CancelInvitationCommand>
    {
        public CancelInvitationValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.InvitationId).NotEmpty();
        }
    }
}