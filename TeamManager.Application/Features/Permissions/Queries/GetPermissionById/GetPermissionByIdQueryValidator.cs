using FluentValidation;

namespace TeamManager.Application.Features.Permissions.Queries.GetPermissionById
{
    public sealed class GetPermissionByIdQueryValidator : AbstractValidator<GetPermissionByIdQuery>
    {
        public GetPermissionByIdQueryValidator()
        {
            RuleFor(x => x.PermissionId).GreaterThan(0);
        }
    }
}
