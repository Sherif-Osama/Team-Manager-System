using MediatR;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.Queries.GetUsers
{
    public sealed record GetUsersQuery(string? Search, bool? IsActive, bool? IsEmailConfirmed, int Page = 1,
        int PageSize = 20) : IRequest<GetUsersResponse>, IRequiresConfirmedEmail;
}