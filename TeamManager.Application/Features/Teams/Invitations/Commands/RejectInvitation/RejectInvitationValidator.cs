using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.RejectInvitation
{
    public sealed class RejectInvitationValidator : AbstractValidator<RejectInvitationCommand>
    {
        public RejectInvitationValidator()
        {
            RuleFor(x => x.Token).Must(T => !string.IsNullOrWhiteSpace(T)).NotEmpty().MaximumLength(500);
        }
    }
}