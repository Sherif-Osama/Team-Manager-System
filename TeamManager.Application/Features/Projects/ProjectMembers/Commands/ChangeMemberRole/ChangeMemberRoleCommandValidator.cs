using FluentValidation;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.ChangeMemberRole
{
    public sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
    {
        public ChangeMemberRoleCommandValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();

            RuleFor(x => x.ProjectMemberId).GreaterThan(0);

            RuleFor(x => x.Role).IsInEnum();
        }
    }
}