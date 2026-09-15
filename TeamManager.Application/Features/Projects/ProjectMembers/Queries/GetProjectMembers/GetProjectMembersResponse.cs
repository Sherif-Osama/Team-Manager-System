using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetProjectMembers
{
    public sealed record GetProjectMembersResponse(IReadOnlyList<ProjectMemberListItem> Members, int TotalCount, int Page = 1,
        int PageSize = 20);

    public sealed record ProjectMemberListItem(Guid UserId, string DisplayName, ProjectRole ProjectRole, ProjectMemberStatus Status,
        DateTime AddedAtUtc, Guid? AddedBy, string? AddedByName, DateTime? RemovedAtUtc, Guid? RemovedBy, string? RemovedByName);
}