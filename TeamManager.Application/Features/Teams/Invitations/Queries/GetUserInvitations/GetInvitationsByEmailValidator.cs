using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations
{
    public sealed class GetInvitationsByEmailValidator : AbstractValidator<GetInvitationsByEmailQuery>
    {
        public GetInvitationsByEmailValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty();

            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

            RuleFor(x => x.Page).GreaterThan(0);

            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}