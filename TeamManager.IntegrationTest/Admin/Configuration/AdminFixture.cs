using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Admin.Configuration
{
    public sealed class AdminFixture : TeamManagerApiFactory
    {
        public AdminFixture() : base("teammanager-admin-tests") { }

        public const string AdminSecret = "YOUR_NEW_ADMIN_SECRET";
    }
}
