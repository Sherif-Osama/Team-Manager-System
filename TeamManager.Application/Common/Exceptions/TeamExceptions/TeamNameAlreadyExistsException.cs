namespace TeamManager.Application.Common.Exceptions.TeamExceptions
{
    public sealed class TeamNameAlreadyExistsException : ApplicationExceptionBase
    {
        public TeamNameAlreadyExistsException(string name) : base($"A team with the name '{name}' already exists.") { }
    }
}