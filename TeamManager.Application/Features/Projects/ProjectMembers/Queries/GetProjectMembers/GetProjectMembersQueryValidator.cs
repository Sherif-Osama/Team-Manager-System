using FluentValidation;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetProjectMembers
{
    public sealed class GetProjectMembersQueryValidator : AbstractValidator<GetProjectMembersQuery>
    {
        public GetProjectMembersQueryValidator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();

            RuleFor(x => x.Search).MaximumLength(150);

            RuleFor(x => x.Role).IsInEnum();

            RuleFor(x => x.Status).IsInEnum();

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}