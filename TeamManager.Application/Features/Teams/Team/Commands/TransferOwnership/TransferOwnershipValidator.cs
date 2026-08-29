using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Commands.TransferOwnership
{
    public sealed class TransferOwnershipValidator : AbstractValidator<TransferOwnershipCommand>
    {
        public TransferOwnershipValidator()
        {
            RuleFor(C => C.TeamId).NotEmpty();

            RuleFor(C => C.NewOwnerUserId).NotEmpty();
        }
    }
}
