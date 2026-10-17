namespace TeamManager.Application.Features.Permissions.Queries
{
    public sealed record GetPermissionsResponse(IReadOnlyList<PermissionItemResponse> Permissions);

    public sealed record PermissionItemResponse(int PermissionId, string Code, string? Description);
}