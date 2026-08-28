using FluentValidation;

namespace TeamManager.Application.Features.Teams.Queries.GetTeamByName
{
    public sealed class GetTeamByNameValidator : AbstractValidator<GetTeamByNameQuery>
    {
        public GetTeamByNameValidator()
        {
            RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(100);
        }
    }
}
