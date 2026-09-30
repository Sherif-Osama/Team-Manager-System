namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabels
{
    public sealed record GetLabelsRequest(string? Search = null, int Page = 1, int PageSize = 20);
}