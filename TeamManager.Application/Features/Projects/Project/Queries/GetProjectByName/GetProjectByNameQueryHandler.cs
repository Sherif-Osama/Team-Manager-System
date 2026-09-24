using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjectByName
{
    public sealed class GetProjectByNameQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetProjectByNameQuery, GetProjectByNameResponse>
    {
        public async Task<GetProjectByNameResponse> Handle(GetProjectByNameQuery request, CancellationToken cancellationToken)
        {
            var project = await context.Projects.AsNoTracking().Where(x => x.Name == request.Name && x.DeletedAtUtc == null)
                .Select(x => new GetProjectByNameResponse(x.Id, x.TeamId, x.Name, x.Team.Name, x.Description, x.Status, x.StartDate,
                x.DueDate, x.OwnerUserId, x.Owner.DisplayName, x.CreatedBy, x.Creator.DisplayName,
                x.Members.Count(m => ProjectMemberStatuses.Occupied.Contains(m.Status)), x.Tasks.Count(t => t.DeletedAtUtc == null),
                x.CreatedAtUtc, x.UpdatedAtUtc)).FirstOrDefaultAsync(cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(request.Name);

            return project;
        }
    }
}