using FluentValidation;
using TeamManager.Application.Features.Teams.Team.Queries.GetTeamByName;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjectByName
{
    public sealed class GetProjectByNameValidator : AbstractValidator<GetTeamByNameQuery>
    {
        public GetProjectByNameValidator()
        {
            RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(100);
        }
    }
}