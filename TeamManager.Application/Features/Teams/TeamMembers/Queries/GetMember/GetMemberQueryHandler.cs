using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember
{
    public sealed class GetMemberQueryHandler(ITeamRepository teamRepository)
        : IRequestHandler<GetMemberQuery, GetMemberResponse>
    {
        public async Task<GetMemberResponse> Handle(GetMemberQuery request, CancellationToken cancellationToken)
        {
            if (!await teamRepository.ExistsAsync(request.TeamId, cancellationToken))
                throw new TeamNotFoundException(request.TeamId);

            var member = await teamRepository.GetMemberAsync(request.TeamId, request.MemberId, cancellationToken);

            if (member is null)
                throw new TeamMemberNotFoundException(request.TeamId, request.MemberId);

            return member;
        }
    }
}