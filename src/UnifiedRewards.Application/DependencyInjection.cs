using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UnifiedRewards.Application.Common.Behaviors;

namespace UnifiedRewards.Application;

/// <summary>Registers MediatR (with the CQRS pipeline) and FluentValidation validators.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
