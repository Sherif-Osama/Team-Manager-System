using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers
{
    public sealed class GetMembersValidator : AbstractValidator<GetMembersQuery>
    {
        public GetMembersValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
