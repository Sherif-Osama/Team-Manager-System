using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Teams.Configuration
{
    public sealed class TeamsFixture : TeamManagerApiFactory
    {
        public TeamsFixture() : base("teammanager-team-tests") { }
    }
}
