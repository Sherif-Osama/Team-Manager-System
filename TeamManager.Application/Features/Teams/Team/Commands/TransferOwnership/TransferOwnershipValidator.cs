using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Commands.TransferOwnership
{
    public sealed class TransferOwnershipValidator : AbstractValidator<TransferOwnershipCommand>
    {
        public TransferOwnershipValidator()
        {
            RuleFor(C => C.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(C => C.NewOwnerUserId)
                .NotEmpty()
                .WithMessage("New owner user ID is required.");
        }
    }
}
