using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Authentication.Configuration
{
    public sealed class AuthenticationFixture : TeamManagerApiFactory
    {
        public AuthenticationFixture() : base("teammanager-authentication-tests") { }
    }
}