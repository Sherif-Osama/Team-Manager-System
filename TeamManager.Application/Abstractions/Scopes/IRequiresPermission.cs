namespace TeamManager.Application.Abstractions.Scopes
{
    public interface IRequiresPermission
    {
        string PermissionCode { get; }
    }
}