using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TeamManager.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace TeamManager.IntegrationTest.Infrastructure
{
    public sealed class TeamManagerApiFactory : WebApplicationFactory<Api.Program>, IAsyncLifetime
    {
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .WithPassword("Test_password!123").WithDatabase("TeamManager_Test").Build();


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
                        ["RateLimiting:Global:PerIpLimit"] = "10000",
                        ["RateLimiting:Authentication:Register:PermitLimit"] = "1000"
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

            // Start the application and apply real EF Core migrations.
            using var scope = Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            await db.Database.MigrateAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();

            await _msSqlContainer.DisposeAsync();
        }
    }
}