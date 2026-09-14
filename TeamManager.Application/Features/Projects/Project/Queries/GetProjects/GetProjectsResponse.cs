using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjects
{
    public sealed record GetProjectsResponse(IReadOnlyList<ProjectListItem> Projects, int TotalCount, int Page = 1, int PageSize = 20);

    public sealed record ProjectListItem(Guid Id, Guid TeamId, string ProjectName, string TeamName, string? Description,
        ProjectStatus Status, DateOnly? StartDate, DateOnly? DueDate, Guid OwnerUserId, string OwnerName,
        Guid CreatedBy, string CreatorName, int MembersCount, int TasksCount, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
}