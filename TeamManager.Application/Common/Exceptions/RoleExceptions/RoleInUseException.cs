namespace TeamManager.Application.Common.Exceptions.RoleExceptions
{
    public sealed class RoleInUseException : ApplicationExceptionBase
    {
        public RoleInUseException(int roleId) : base($"Role with id '{roleId}' is assigned to users and cannot be deleted.") { }
    }
}
