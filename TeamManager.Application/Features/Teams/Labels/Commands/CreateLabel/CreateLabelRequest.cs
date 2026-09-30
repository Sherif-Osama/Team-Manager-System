namespace TeamManager.Application.Features.Teams.Labels.Commands.CreateLabel
{
    public sealed record CreateLabelRequest(string Name, string? ColorHex = null);
}