using FluentValidation;

namespace TeamManager.Application.Features.Projects.Project.Commands.TransferProjectOwnership
{
    public sealed class TransferProjectOwnershipCommandValidator : AbstractValidator<TransferProjectOwnershipCommand>
    {
        public TransferProjectOwnershipCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.NewOwnerUserId)
                .NotEmpty()
                .WithMessage("New owner user ID is required.");
        }
    }
}
