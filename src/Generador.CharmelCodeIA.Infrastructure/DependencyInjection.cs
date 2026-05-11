using Generador.CharmelCodeIA.Domain.Interfaces;
using Generador.CharmelCodeIA.Infrastructure.AI;
using Generador.CharmelCodeIA.Infrastructure.AI.Skills;
using Generador.CharmelCodeIA.Infrastructure.DatabaseSchema;
using Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.NoSql;
using Generador.CharmelCodeIA.Infrastructure.DifferentialAnalysis;
using Generador.CharmelCodeIA.Infrastructure.ProjectScaffolding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Generador.CharmelCodeIA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISchemaInferenceStrategy, SchemaInferenceEngine>();
        services.AddSingleton<ISchemaReaderFactory, SchemaReaderFactory>();
        services.AddSingleton<ITemplateEngine, TemplateEngine>();

        // Scaffolding
        services.AddSingleton<SolutionScaffolder>();
        services.AddSingleton<LayerScaffolder>();
        services.AddSingleton<IncrementalScaffolder>();
        services.AddSingleton<IProjectScaffolder, ProjectScaffolder>();

        // Differential
        services.AddSingleton<ProjectFileScanner>();
        services.AddSingleton<ConflictResolver>();
        services.AddSingleton<IDifferentialAnalyzer, DifferentialAnalyzer>();

        return services;
    }

    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services, Action<AiModelConfig> configureModel)
    {
        var config = new AiModelConfig();
        configureModel(config);

        services.AddSingleton(config);

        services.AddSingleton<Kernel>(sp =>
        {
            var modelConfig = sp.GetRequiredService<AiModelConfig>();
            var kernel = KernelFactory.Create(modelConfig);

            var databaseSkill = new DatabaseSkill(sp);
            var entitySkill = new EntitySkill(sp);
            var scaffoldSkill = new ScaffoldSkill(sp);
            var useCaseSkill = new UseCaseSkill(sp);

            kernel.Plugins.AddFromObject(databaseSkill, "Database");
            kernel.Plugins.AddFromObject(entitySkill, "Entity");
            kernel.Plugins.AddFromObject(scaffoldSkill, "Scaffold");
            kernel.Plugins.AddFromObject(useCaseSkill, "UseCase");

            return kernel;
        });

        services.AddSingleton<DatabaseSkill>();
        services.AddSingleton<EntitySkill>();
        services.AddSingleton<ScaffoldSkill>();
        services.AddSingleton<UseCaseSkill>();

        return services;
    }
}
