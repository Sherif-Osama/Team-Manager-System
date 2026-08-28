using FluentValidation;

namespace TeamManager.Application.Features.Teams.Queries.GetTeam
{

    public sealed class GetTeamQueryValidator : AbstractValidator<GetTeamQuery>
    {
        public GetTeamQueryValidator()
        {
            RuleFor(C => C.TeamId).NotEmpty();
        }
    }
}