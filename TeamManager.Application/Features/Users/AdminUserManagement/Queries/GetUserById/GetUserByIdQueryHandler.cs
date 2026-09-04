using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetUserByIdQuery, GetUserByIdResponse>
    {
        public async Task<GetUserByIdResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await context.Users.AsNoTracking().Where(x => x.Id == request.UserId && x.DeletedAtUtc == null)
                .Select(x => new GetUserByIdResponse(x.Id, x.Email, x.DisplayName, x.IsEmailConfirmed, x.IsActive, x.LastLoginUtc,
                x.CreatedAtUtc, context.TeamMembers.Count(member => member.UserId == x.Id &&
                member.Status == TeamMemberStatus.Active && member.RemovedAtUtc == null),
                context.Teams.Count(team => team.OwnerUserId == x.Id && team.DeletedAtUtc == null), x.UserRoles.Select(ur => ur.Role.Name).ToList())).FirstOrDefaultAsync(cancellationToken);

            if (user is null)
                throw new UserNotFoundException(request.UserId);

            return user;
        }
    }
}