using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember
{
    public sealed class GetMemberValidator
        : AbstractValidator<GetMemberQuery>
    {
        public GetMemberValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.MemberId).GreaterThan(0);
        }
    }
}