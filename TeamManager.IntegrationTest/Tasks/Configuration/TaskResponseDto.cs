using TeamManager.Domain.Enums;

namespace TeamManager.IntegrationTest.Tasks.Configuration
{
    public sealed record GetTaskResponseDto(long Id, Guid ProjectId, string ProjectName, string Title, string? Description,
        TaskItemStatus Status, TaskPriority Priority, Guid CreatedBy, string CreatorName, Guid? AssigneeUserId, string? AssigneeName,
        DateOnly? StartDate, DateOnly? DueDate, DateTime? CompletedAtUtc, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
        int DependenciesCount, int LabelsCount, int ChecklistItemsCount, int AttachmentsCount, int CommentsCount);
}
