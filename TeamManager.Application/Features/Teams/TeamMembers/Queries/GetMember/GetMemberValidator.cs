using FluentValidation;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember
{
    public sealed class GetMemberValidator
        : AbstractValidator<GetMemberQuery>
    {
        public GetMemberValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.MemberId)
                .GreaterThan(0)
                .WithMessage("Member ID must be greater than 0.");
        }
    }
}
