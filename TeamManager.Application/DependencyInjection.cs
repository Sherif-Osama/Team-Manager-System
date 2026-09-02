using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TeamManager.Application.Common.Behaviors;

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

                configuration.AddOpenBehavior(typeof(ConfirmedEmailBehavior<,>));

                configuration.AddOpenBehavior(typeof(TeamAuthorizationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}