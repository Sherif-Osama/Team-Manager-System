using TeamManager.Domain.Enums;

namespace TeamManager.IntegrationTest.Projects.Configuration
{
    public sealed record ProjectResponseDto(Guid Id, Guid TeamId, string ProjectName, string TeamName, string? Description,
        ProjectStatus Status, DateOnly? StartDate, DateOnly? DueDate, Guid OwnerUserId, string OwnerName,
        Guid CreatedBy, string CreatorName, int MembersCount, int TasksCount, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

    public sealed record GetProjectsResponseDto(IReadOnlyList<ProjectResponseDto> Projects, int TotalCount, int Page, int PageSize);

    public sealed record ProjectMemberItemDto(Guid UserId, string DisplayName, ProjectRole ProjectRole, ProjectMemberStatus Status,
        DateTime AddedAtUtc, Guid? AddedBy, string? AddedByName, DateTime? RemovedAtUtc, Guid? RemovedBy, string? RemovedByName);

    public sealed record GetProjectMembersResponseDto(IReadOnlyList<ProjectMemberItemDto> Members, int TotalCount, int Page,
        int PageSize);
}
