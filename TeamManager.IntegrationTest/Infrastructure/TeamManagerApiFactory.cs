using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Respawn;
using Respawn.Graph;
using TeamManager.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace TeamManager.IntegrationTest.Infrastructure
{
    public class TeamManagerApiFactory(string containerName) : WebApplicationFactory<Api.Program>, IAsyncLifetime
    {
        private Respawner _respawner = null!;
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").WithName(containerName).WithPassword("Test_password!123")
            .WithDatabase("TeamManager_Test").Build();

        static TeamManagerApiFactory()
        {
            // AddApiRateLimiting captures the global limits while services are registered, before this
            // class's configuration sources are applied, so only environment variables reach it.
            Environment.SetEnvironmentVariable("RateLimiting__Global__PerIpLimit", "1000000");
            Environment.SetEnvironmentVariable("RateLimiting__Global__PerIpWindowSeconds", "6");
            Environment.SetEnvironmentVariable("RateLimiting__Global__PerUserLimit", "1000000");
            Environment.SetEnvironmentVariable("RateLimiting__Global__PerUserWindowSeconds", "6");
        }

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "integration-tests/1.0");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Key"] = "test-only-signing-key-at-least-32-characters!",
                            ["Jwt:Issuer"] = "TeamManager.Tests",
                            ["Jwt:Audience"] = "TeamManager.Tests",

                            ["RateLimiting:Authentication:Register:PermitLimit"] = "100000",
                            ["RateLimiting:Authentication:Register:WindowSeconds"] = "6",

                            ["RateLimiting:Authentication:Login:PermitLimit"] = "100000",
                            ["RateLimiting:Authentication:Login:WindowSeconds"] = "6",

                            ["RateLimiting:Authentication:Refresh:PermitLimit"] = "100000",
                            ["RateLimiting:Authentication:Refresh:WindowSeconds"] = "6",

                            ["RateLimiting:Authentication:ForgotPassword:PermitLimit"] = "100000",
                            ["RateLimiting:Authentication:ForgotPassword:WindowSeconds"] = "6",

                            ["RateLimiting:Authentication:ResetPassword:PermitLimit"] = "100000",
                            ["RateLimiting:Authentication:ResetPassword:WindowSeconds"] = "6",

                            ["Bootstrap:AdminSecret"] = "YOUR_NEW_ADMIN_SECRET"
                        });
                });

            builder.ConfigureTestServices(services =>
              {
                  services.RemoveAll<DbContextOptions<TeamManagerDbContext>>();
                  services.AddDbContext<TeamManagerDbContext>(options =>
                  options.UseSqlServer(_msSqlContainer.GetConnectionString()));

                  // Disable background jobs for regular API integration tests.
                  var backgroundJobs = services.Where(descriptor =>
                  descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType?.Namespace?.StartsWith(
                              "TeamManager.Infrastructure.BackgroundJobs", StringComparison.Ordinal) == true).ToList();

                  foreach (var job in backgroundJobs)
                  {
                      services.Remove(job);
                  }
              }
            );

        }

        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();

            await using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());

            await connection.OpenAsync();
            // Start the application and apply real EF Core migrations.
            using var scope = Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            await db.Database.MigrateAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                TablesToIgnore = [new Table("__EFMigrationsHistory"), new Table("Roles"), new Table("Permissions"), new Table("RolePermissions")]
            });
        }

        public async Task ResetDatabaseAsync()
        {
            await using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();

            await _msSqlContainer.DisposeAsync();
        }
    }
}