namespace TeamManager.Application.Common.Exceptions
{
    public sealed class TeamNotFoundException : ApplicationExceptionBase
    {
        public TeamNotFoundException(Guid teamId) : base($"The team with id '{teamId}' was not found.") { }

        public TeamNotFoundException(string name) : base($"The team with name '{name}' was not found.") { }
    }
}