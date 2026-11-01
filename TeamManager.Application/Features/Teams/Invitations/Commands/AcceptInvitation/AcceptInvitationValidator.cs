using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.AcceptInvitation
{
    public sealed class AcceptInvitationValidator : AbstractValidator<AcceptInvitationCommand>
    {
        public AcceptInvitationValidator()
        {
            RuleFor(c => c.Token)
                .NotEmpty()
                .WithMessage("Invitation token is required.")
                .Must(T => !string.IsNullOrWhiteSpace(T))
                .WithMessage("Invitation token cannot be whitespace.")
                .MaximumLength(500)
                .WithMessage("Invitation token must not exceed 500 characters.");
        }
    }
}
