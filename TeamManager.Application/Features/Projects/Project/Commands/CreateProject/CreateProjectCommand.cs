using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Commands.CreateProject
{
    public sealed record CreateProjectCommand(Guid TeamId, string Name, string? Description = null,
        DateOnly? StartDate = null, DateOnly? DueDate = null) : IRequest<Guid>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner];
    }
}