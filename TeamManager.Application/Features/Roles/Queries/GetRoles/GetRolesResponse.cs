namespace TeamManager.Application.Features.Roles.Queries.GetRoles
{
    public sealed record GetRolesResponse(IReadOnlyList<RoleListItem> Roles);

    public sealed record RoleListItem(int RoleId, string Name, string? Description);
}