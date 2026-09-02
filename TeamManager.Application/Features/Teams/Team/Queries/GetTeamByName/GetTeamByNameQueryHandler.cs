using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeamByName
{
    public sealed class GetTeamByNameQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetTeamByNameQuery, GetTeamByNameResponse>
    {

        public async Task<GetTeamByNameResponse> Handle(GetTeamByNameQuery request, CancellationToken cancellationToken)
        {
            var team = await context.Teams.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == request.Name && x.DeletedAtUtc == null, cancellationToken);


            if (team is null)
                throw new TeamNotFoundException(request.Name);

            return new GetTeamByNameResponse(team.Id, team.Name, team.Description,
                team.IsActive, team.OwnerUserId, team.CreatedBy, team.CreatedAtUtc);
        }
    }
}