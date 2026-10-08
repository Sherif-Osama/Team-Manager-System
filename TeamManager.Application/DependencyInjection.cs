using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TeamManager.Application.Abstractions.Realtime;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Realtime;

namespace TeamManager.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));

                configuration.AddOpenBehavior(typeof(PermissionAuthorizationBehavior<,>));
                configuration.AddOpenBehavior(typeof(ConfirmedEmailBehavior<,>));
                configuration.AddOpenBehavior(typeof(TeamAuthorizationBehavior<,>));
                configuration.AddOpenBehavior(typeof(ProjectAuthorizationBehavior<,>));
                configuration.AddOpenBehavior(typeof(TaskAuthorizationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
            services.AddScoped<IPostCommitNotificationDispatcher, PostCommitNotificationDispatcher>();
            return services;
        }
    }
}