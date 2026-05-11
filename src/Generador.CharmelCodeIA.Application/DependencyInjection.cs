using FluentValidation;
using Generador.CharmelCodeIA.Application.Common.Behaviors;
using Generador.CharmelCodeIA.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Generador.CharmelCodeIA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton<SchemaAnalyzer>();
        services.AddSingleton<GenerationOrchestrator>();
        services.AddSingleton<DifferentialService>();

        return services;
    }
}
