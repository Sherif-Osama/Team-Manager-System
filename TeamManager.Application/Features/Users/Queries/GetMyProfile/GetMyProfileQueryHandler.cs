using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Users.Queries.GetMyProfile
{
    public sealed class GetMyProfileQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
        : IRequestHandler<GetMyProfileQuery, GetMyProfileResponse>
    {
        public async Task<GetMyProfileResponse> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await context.Users.AsNoTracking().Where(x => x.Id == currentUser.UserId.Value && x.DeletedAtUtc == null)
                .Select(x => new GetMyProfileResponse(x.Id, x.Email, x.DisplayName, x.IsEmailConfirmed,
                x.IsActive, x.LastLoginUtc, x.CreatedAtUtc, context.TeamMembers.Count(x => x.UserId == currentUser.UserId.Value
            && x.RemovedAtUtc == null && x.Status == TeamMemberStatus.Active), context.Teams.Count(x => x.OwnerUserId == currentUser.UserId.Value &&
            x.DeletedAtUtc == null))).FirstOrDefaultAsync(cancellationToken);

            if (user is null)
                throw new UserNotFoundException(currentUser.UserId.Value);

            return user;
        }
    }
}