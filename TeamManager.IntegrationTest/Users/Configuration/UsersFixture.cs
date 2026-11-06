using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Users.Configuration
{
    public sealed class UsersFixture : TeamManagerApiFactory
    {
        public UsersFixture() : base("teammanager-users-tests") { }
    }
}
