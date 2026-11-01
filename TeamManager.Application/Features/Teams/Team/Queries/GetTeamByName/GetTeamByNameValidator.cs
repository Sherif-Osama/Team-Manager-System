using FluentValidation;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeamByName
{
    public sealed class GetTeamByNameValidator : AbstractValidator<GetTeamByNameQuery>
    {
        public GetTeamByNameValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Team name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Team name cannot be whitespace.")
                .MaximumLength(100)
                .WithMessage("Team name must not exceed 100 characters.");
        }
    }
}
