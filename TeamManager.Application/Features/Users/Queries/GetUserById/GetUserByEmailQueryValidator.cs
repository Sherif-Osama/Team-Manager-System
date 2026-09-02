using FluentValidation;
using TeamManager.Application.Features.Users.Queries.GetUserByEmail;

namespace TeamManager.Application.Features.Users.Queries.GetUserById
{
    public sealed class GetUserByEmailQueryValidator : AbstractValidator<GetUserByEmailQuery>
    {
        public GetUserByEmailQueryValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}