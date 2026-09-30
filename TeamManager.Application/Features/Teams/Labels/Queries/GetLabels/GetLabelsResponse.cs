namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabels
{
    public sealed record GetLabelsResponse(IReadOnlyCollection<GetLabelsItemResponse> Items, int Page,
        int PageSize, int TotalCount);

    public sealed record GetLabelsItemResponse(long LabelId, string Name, string ColorHex, DateTime CreatedAtUtc);
}