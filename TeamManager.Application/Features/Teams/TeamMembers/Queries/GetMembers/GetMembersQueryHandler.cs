using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers
{
    public sealed class GetMembersQueryHandler(ITeamRepository teamRepository)
        : IRequestHandler<GetMembersQuery, GetMembersResponse>
    {
        public async Task<GetMembersResponse> Handle(GetMembersQuery request, CancellationToken cancellationToken)
        {
            if (!await teamRepository.ExistsAsync(request.TeamId, cancellationToken))
                throw new TeamNotFoundException(request.TeamId);

            return await teamRepository.GetMembersAsync(request.TeamId, request.Page, request.PageSize, cancellationToken);
        }
    }
}