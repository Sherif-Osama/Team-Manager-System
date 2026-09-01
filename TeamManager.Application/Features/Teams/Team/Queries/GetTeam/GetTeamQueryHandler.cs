using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeam
{
    public sealed class GetTeamQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetTeamQuery, GetTeamResponse>
    {
        public async Task<GetTeamResponse> Handle(GetTeamQuery request, CancellationToken cancellationToken)
        {
            var team = await context.Teams.AsNoTracking().Where(x => x.Id == request.TeamId && x.DeletedAtUtc == null)
              .Select(x => new GetTeamResponse(x.Id, x.Name, x.Description, x.IsActive, x.OwnerUserId,
              x.Owner.DisplayName, x.CreatedBy, x.Creator.DisplayName, x.Members.Count, x.Projects.Count,
              x.CreatedAtUtc, x.UpdatedAtUtc)).FirstOrDefaultAsync(cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            return team;
        }
    }
}