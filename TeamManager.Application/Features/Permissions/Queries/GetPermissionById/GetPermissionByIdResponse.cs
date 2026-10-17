namespace TeamManager.Application.Features.Permissions.Queries.GetPermissionById
{
    public sealed record GetPermissionByIdResponse(int PermissionId, string Code, string? Description);
}
