using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.AcceptInvitation
{
    public sealed class AcceptInvitationValidator : AbstractValidator<AcceptInvitationCommand>
    {
        public AcceptInvitationValidator()
        {
            RuleFor(c => c.Token).Must(T => !string.IsNullOrWhiteSpace(T)).NotEmpty().MaximumLength(500);
        }
    }
}