namespace TeamManager.Application.Common.Authorization.Scopes
{
    public interface IRequiresPermission
    {
        string PermissionCode { get; }
    }
}