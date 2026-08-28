namespace TeamManager.Application.Common.Exceptions
{
    public sealed class TeamNameAlreadyExistsException : ApplicationExceptionBase
    {
        public TeamNameAlreadyExistsException(string name) : base($"A team with the name '{name}' already exists.") { }
    }
}