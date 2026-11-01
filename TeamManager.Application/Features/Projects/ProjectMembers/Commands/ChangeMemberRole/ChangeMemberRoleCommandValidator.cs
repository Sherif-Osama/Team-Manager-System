using FluentValidation;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
    {
        public ChangeMemberRoleCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.ProjectMemberId)
                .GreaterThan(0)
                .WithMessage("Project member ID must be greater than 0.");

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid project role.");
        }
    }
}
