using FluentValidation;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.RemoveMember
{
    public sealed class RemoveProjectMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
    {
        public RemoveProjectMemberCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.MemberId)
                .NotEmpty()
                .WithMessage("Member ID is required.")
                .GreaterThan(0)
                .WithMessage("Member ID must be greater than 0.");
        }
    }
}
