using FluentValidation;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations
{
    public sealed class GetInvitationsValidator : AbstractValidator<GetInvitationsQuery>
    {
        public GetInvitationsValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Team ID is required.");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");

            RuleFor(x => x.Search)
                .MaximumLength(256)
                .WithMessage("Search term must not exceed 256 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Search));

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid invitation status.")
                .When(x => x.Status.HasValue);

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Invalid team role.")
                .When(x => x.Role.HasValue);
        }
    }
}
