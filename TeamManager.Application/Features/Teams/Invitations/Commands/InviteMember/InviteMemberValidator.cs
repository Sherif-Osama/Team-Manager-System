using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.InviteMember
{
    public sealed class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
    {
        public InviteMemberValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .MaximumLength(256)
                .WithMessage("Email must not exceed 256 characters.")
                .EmailAddress()
                .WithMessage("Email format is invalid.");

            RuleFor(x => x.TeamRole)
                .IsInEnum()
                .WithMessage("Invalid team role.");
        }
    }
}
