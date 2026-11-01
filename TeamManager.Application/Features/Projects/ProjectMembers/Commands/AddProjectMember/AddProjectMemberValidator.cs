using FluentValidation;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Commands.AddProjectMember
{
    public sealed class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
    {
        public AddProjectMemberValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.ProjectRole)
                .IsInEnum()
                .WithMessage("Invalid project role.");
        }
    }
}
