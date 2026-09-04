namespace TeamManager.Application.Common.Authorization
{
    public interface IRequiresPermission
    {
        string PermissionCode { get; }
    }
}