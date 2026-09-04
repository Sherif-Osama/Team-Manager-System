using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Configuration;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Infrastructure.BackgroundJobs.DeleteInactiveUsers;
using TeamManager.Infrastructure.BackgroundJobs.InvitationExpiration;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.MessageHandlers;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages;
using TeamManager.Infrastructure.Communication;
using TeamManager.Infrastructure.Persistence;
using TeamManager.Infrastructure.Persistence.Outbox;
using TeamManager.Infrastructure.Persistence.Repositories;
using TeamManager.Infrastructure.Services.AuthenticationServices;
using TeamManager.Infrastructure.Services.SecurityService;

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
            services.AddHostedService<ExpirePendingInvitationsJob>();
            services.AddScoped<InvitationExpirationService>();
            services.AddScoped<IOutbox, Outbox>();
            services.AddHostedService<ProcessOutboxMessagesJob>();
            services.AddScoped<OutboxProcessorService>();
            services.AddScoped<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
            services.AddScoped<IOutboxMessageHandler, InvitationEmailOutboxMessageHandler>();
            services.AddScoped<IOutboxMessageHandler, EmailConfirmationOutboxMessageHandler>();
            services.AddScoped<IOutboxMessageHandler, PasswordChangedNotificationOutboxMessageHandler>();
            services.AddScoped<IOutboxMessageHandler, AccountDeletedEmailHandler>();
            services.AddScoped<IOutboxMessageHandler, AccountDeactivatedEmailHandler>();
            services.AddScoped<IOutboxMessageHandler, AccountActivatedEmailHandler>();
            services.AddScoped<InactiveUserDeletionService>();
            services.AddHostedService<DeleteInactiveUsersJob>();
            services.Configure<BootstrapOptions>(configuration.GetSection(BootstrapOptions.SectionName));
            services.AddScoped<IBootstrapSecretProvider, BootstrapSecretProvider>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            return services;
        }
    }
}