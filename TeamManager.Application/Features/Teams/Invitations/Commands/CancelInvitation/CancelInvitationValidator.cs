using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.CancelInvitation
{
    public sealed class CancelInvitationValidator : AbstractValidator<CancelInvitationCommand>
    {
        public CancelInvitationValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.InvitationId)
                .NotEmpty()
                .WithMessage("Invitation ID is required.");
        }
    }
}
