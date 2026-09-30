namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabel
{
    public sealed record GetLabelByIdResponse(long LabelId, Guid TeamId, string Name, string ColorHex, DateTime CreatedAtUtc);
}