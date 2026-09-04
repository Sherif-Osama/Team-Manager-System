namespace TeamManager.Application.Common.Exceptions
{
    public class RoleNotFoundException : ApplicationExceptionBase
    {
        public RoleNotFoundException(int roleId) : base($"Role with ID {roleId} not found.") { }
    }
}