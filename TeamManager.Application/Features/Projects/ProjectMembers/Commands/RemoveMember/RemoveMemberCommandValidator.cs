using FluentValidation;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.RemoveMember
{
    public sealed class RemoveProjectMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
    {
        public RemoveProjectMemberCommandValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();

            RuleFor(x => x.MemberId).NotEmpty().GreaterThan(0);
        }
    }
}