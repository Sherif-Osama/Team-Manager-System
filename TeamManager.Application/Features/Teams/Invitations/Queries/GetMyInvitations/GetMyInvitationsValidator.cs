using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetMyInvitations
{
    public sealed class GetMyInvitationsValidator : AbstractValidator<GetMyInvitationsQuery>
    {
        public GetMyInvitationsValidator()
        {
            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}