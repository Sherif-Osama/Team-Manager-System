using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Projects.Configuration
{
    public sealed class ProjectsFixture : TeamManagerApiFactory
    {
        public ProjectsFixture() : base("teammanager-projects-tests") { }
    }
}
