namespace TeamManager.IntegrationTest.Infrastructure
{
    public abstract class IntegrationTestBase(TeamManagerApiFactory factory) : IAsyncLifetime
    {
        public Task InitializeAsync() => factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
