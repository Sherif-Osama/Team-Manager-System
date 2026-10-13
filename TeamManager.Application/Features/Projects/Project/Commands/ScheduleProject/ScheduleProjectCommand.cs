using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.ScheduleProject
{
    public sealed record ScheduleProjectCommand(Guid ProjectId, DateOnly? StartDate, DateOnly? DueDate)
        : IRequest, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner];
    }
}