namespace TeamManager.Application.Common.Exceptions.AuthorizationExceptions
{
    public class RoleNotFoundException : ApplicationExceptionBase
    {
        public RoleNotFoundException(int roleId) : base($"Role with ID {roleId} not found.") { }
    }
}