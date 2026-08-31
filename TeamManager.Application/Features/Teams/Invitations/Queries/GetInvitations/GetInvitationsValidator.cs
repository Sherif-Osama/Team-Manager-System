using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations
{
    public sealed class GetInvitationsValidator : AbstractValidator<GetInvitationsQuery>
    {
        public GetInvitationsValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.Search).MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Search));

            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

            RuleFor(x => x.role).IsInEnum().When(x => x.role.HasValue);
        }
    }
}