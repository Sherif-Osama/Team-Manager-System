using FluentValidation;


namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember
{
    public sealed class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
    {
        public AddProjectMemberValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();

            RuleFor(x => x.UserId).NotEmpty();

            RuleFor(x => x.ProjectRole).IsInEnum();
        }
    }
}