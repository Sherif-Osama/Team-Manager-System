namespace TeamManager.Application.Common.Exceptions.TeamExceptions
{
    public sealed class TeamHasActiveProjectsException : ApplicationExceptionBase
    {
        public TeamHasActiveProjectsException(Guid teamId)
            : base($"The team with id '{teamId}' cannot be deleted because it still contains active projects.") { }
    }
}