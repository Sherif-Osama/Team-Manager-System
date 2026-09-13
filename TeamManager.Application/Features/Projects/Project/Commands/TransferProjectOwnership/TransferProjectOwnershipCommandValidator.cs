using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.TransferProjectOwnership
{
    public sealed class TransferProjectOwnershipCommandValidator : AbstractValidator<TransferProjectOwnershipCommand>
    {
        public TransferProjectOwnershipCommandValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();

            RuleFor(x => x.NewOwnerUserId).NotEmpty();
        }
    }
}