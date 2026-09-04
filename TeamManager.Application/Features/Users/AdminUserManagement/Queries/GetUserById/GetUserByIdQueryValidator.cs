using FluentValidation;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById
{
    public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdQueryValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}