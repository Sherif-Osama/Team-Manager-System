using FluentValidation;

namespace TeamManager.Application.Features.Users.Queries.GetUserByEmail
{
    public sealed class GetUserByEmailQueryValidator : AbstractValidator<GetUserByEmailQuery>
    {
        public GetUserByEmailQueryValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}