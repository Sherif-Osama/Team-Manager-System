namespace TeamManager.Application.Abstractions.Security
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken);

        Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken);
    }
}