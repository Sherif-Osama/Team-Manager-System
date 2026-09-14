using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProject
{
    public sealed record GetProjectResponse(Guid Id, Guid TeamId, string ProjectName, string TeamName, string? Description,
        ProjectStatus Status, DateOnly? StartDate, DateOnly? DueDate, Guid OwnerUserId, string OwnerName,
        Guid CreatedBy, string CreatorName, int MembersCount, int TasksCount, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc
    );
}