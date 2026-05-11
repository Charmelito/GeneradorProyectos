using Microsoft.SemanticKernel;

namespace Generador.CharmelCodeIA.Infrastructure.AI;

public static class KernelFactory
{
    public static Kernel Create(AiModelConfig config)
    {
        var builder = Kernel.CreateBuilder();

        switch (config.Provider)
        {
            case "DeepSeek":
                builder.AddOpenAIChatCompletion(
                    modelId: config.ModelId,
                    apiKey: config.ApiKey,
                    endpoint: config.Endpoint);
                break;

            case "AzureOpenAI":
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: config.ModelId,
                    apiKey: config.ApiKey,
                    endpoint: config.Endpoint.AbsoluteUri);
                break;

            case "OpenAI":
                builder.AddOpenAIChatCompletion(
                    modelId: config.ModelId,
                    apiKey: config.ApiKey);
                break;

            default:
                throw new ArgumentException($"Unsupported AI provider: {config.Provider}");
        }

        return builder.Build();
    }

    public static Kernel CreateWithPlugins(AiModelConfig config, object databaseSkill, object entitySkill, object scaffoldSkill, object useCaseSkill)
    {
        var builder = Kernel.CreateBuilder();

        switch (config.Provider)
        {
            case "DeepSeek":
                builder.AddOpenAIChatCompletion(
                    modelId: config.ModelId,
                    apiKey: config.ApiKey,
                    endpoint: config.Endpoint);
                break;

            case "AzureOpenAI":
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: config.ModelId,
                    apiKey: config.ApiKey,
                    endpoint: config.Endpoint.AbsoluteUri);
                break;

            case "OpenAI":
                builder.AddOpenAIChatCompletion(
                    modelId: config.ModelId,
                    apiKey: config.ApiKey);
                break;

            default:
                throw new ArgumentException($"Unsupported AI provider: {config.Provider}");
        }

        builder.Plugins.AddFromObject(databaseSkill, "Database");
        builder.Plugins.AddFromObject(entitySkill, "Entity");
        builder.Plugins.AddFromObject(scaffoldSkill, "Scaffold");
        builder.Plugins.AddFromObject(useCaseSkill, "UseCase");

        return builder.Build();
    }
}
