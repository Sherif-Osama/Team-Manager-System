namespace TeamManager.Application.Features.Teams.Labels.Commands.UpdateLabel
{
    public sealed record UpdateLabelRequest(string Name, string? ColorHex = null);
}