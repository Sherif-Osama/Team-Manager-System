using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations
{
    public sealed class GetInvitationsByEmailValidator : AbstractValidator<GetInvitationsByEmailQuery>
    {
        public GetInvitationsByEmailValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .MaximumLength(256)
                .WithMessage("Email must not exceed 256 characters.")
                .EmailAddress()
                .WithMessage("Email format is invalid.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid invitation status.")
                .When(x => x.Status.HasValue);

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
        }
    }
}
