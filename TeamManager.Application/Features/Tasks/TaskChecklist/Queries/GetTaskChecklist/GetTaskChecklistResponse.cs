namespace TeamManager.Application.Features.Tasks.TaskChecklist.Queries.GetTaskChecklist
{
    public sealed record GetTaskChecklistResponse(long Id, long TaskId, string Content, bool IsCompleted, short SortOrder,
        DateTime? CompletedAtUtc, Guid? CompletedBy, string? CompletedByName, DateTime CreatedAtUtc);
}