namespace TeamManager.Application.Common.Exceptions
{
    public sealed class ProjectNameAlreadyExistsException : ApplicationExceptionBase
    {
        public ProjectNameAlreadyExistsException(string name) : base($"A project with the name '{name}' already exists.")
        { }
    }
}