using System.Reflection;
using FluentValidation;
using IndustrialPlatform.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assemblies = new[] { Assembly.GetExecutingAssembly() }.Concat(additionalAssemblies).ToArray();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));

        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
