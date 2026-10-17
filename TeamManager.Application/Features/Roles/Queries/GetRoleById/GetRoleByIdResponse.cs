namespace TeamManager.Application.Features.Roles.Queries.GetRoleById
{
    public sealed record GetRoleByIdResponse(int RoleId, string Name, string? Description,
        IReadOnlyList<RolePermissionItemResponse> Permissions);

    public sealed record RolePermissionItemResponse(int PermissionId, string Code);
}
