namespace TeamManager.Application.Common.Exceptions.RoleExceptions
{
    public sealed class RoleNameAlreadyExistsException : ApplicationExceptionBase
    {
        public RoleNameAlreadyExistsException(string name) : base($"A role with the name '{name}' already exists.") { }
    }
}