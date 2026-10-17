namespace TeamManager.Application.Common.Exceptions.PermissionExceptions
{
    public sealed class PermissionNotFoundException : ApplicationExceptionBase
    {
        public PermissionNotFoundException(int permissionId) : base($"Permission with id '{permissionId}' was not found.") { }
    }
}
