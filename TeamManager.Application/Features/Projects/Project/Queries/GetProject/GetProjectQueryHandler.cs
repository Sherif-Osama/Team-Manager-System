using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProject
{
    public sealed class GetProjectByIdQueryHandler(IApplicationDbContext context) :
        IRequestHandler<GetProjectQuery, GetProjectResponse>
    {
        public async Task<GetProjectResponse> Handle(GetProjectQuery request, CancellationToken cancellationToken)
        {
            var Project = await context.Projects.AsNoTracking().Where(p => p.Id == request.ProjectId && p.DeletedAtUtc == null)
                .Select(p => new GetProjectResponse(p.Id, p.TeamId, p.Name, p.Team.Name, p.Description, p.Status, p.StartDate, p.DueDate,
                p.OwnerUserId, p.Owner.DisplayName, p.CreatedBy, p.Creator.DisplayName,
                p.Members.Count(m => ProjectMemberStatuses.Occupied.Contains(m.Status)),
                p.Tasks.Count(t => t.Status != TaskItemStatus.Cancelled && t.DeletedAtUtc == null), p.CreatedAtUtc, p.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);

            if (Project is null)
                throw new ProjectNotFoundException(request.ProjectId);

            return Project;
        }
    }
}