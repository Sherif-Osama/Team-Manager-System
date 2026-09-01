using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Communication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Infrastructure.Authentication;
using TeamManager.Infrastructure.Communication;
using TeamManager.Infrastructure.Persistence;
using TeamManager.Infrastructure.Persistence.Repositories;
using TeamManager.Infrastructure.Security;

namespace TeamManager.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<TeamManagerDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TeamManagerDbContext>());
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.AddScoped<IAccessTokenService, AccessTokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<ITeamRepository, TeamRepository>();
            services.AddScoped<IInvitationTokenService, InvitationTokenService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
            services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<TeamManagerDbContext>());
            return services;
        }
    }
}