using System.ComponentModel;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.AI.Skills;

public sealed class UseCaseSkill
{
    private readonly IServiceProvider _serviceProvider;

    public UseCaseSkill(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [KernelFunction("generate_command")]
    [Description("Generates a CQRS Command class for a specific entity and action")]
    [return: Description("The generated Command class code")]
    public async Task<string> GenerateCommandAsync(
        [Description("Entity name (e.g., User, Order)")] string entityName,
        [Description("Action name (e.g., Create, Update, Delete)")] string action,
        [Description("Use case type: Command or Query")] string useCaseType,
        [Description("Company name for namespace")] string companyName,
        [Description("Project name for namespace")] string projectName,
        [Description("Custom description for prompt context")] string? description = null)
    {
        var generator = _serviceProvider.GetRequiredService<IUseCaseGenerator>();
        var useCase = new UseCaseDefinition
        {
            EntityName = entityName,
            Action = action,
            Type = Enum.Parse<UseCaseType>(useCaseType, ignoreCase: true),
            Description = description ?? $"{action} {entityName}"
        };

        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(companyName, projectName),
            OutputPath = string.Empty
        };

        var schema = new DatabaseSchemaEntity();

        return useCase.Type == UseCaseType.Command
            ? await generator.GenerateCommandAsync(useCase, schema, config)
            : await generator.GenerateQueryAsync(useCase, schema, config);
    }

    [KernelFunction("generate_handler")]
    [Description("Generates a CQRS Handler class for a specific entity and action")]
    [return: Description("The generated Handler class code")]
    public async Task<string> GenerateHandlerAsync(
        [Description("Entity name")] string entityName,
        [Description("Action name")] string action,
        [Description("Use case type: Command or Query")] string useCaseType,
        [Description("Company name for namespace")] string companyName,
        [Description("Project name for namespace")] string projectName,
        [Description("Custom description for prompt context")] string? description = null)
    {
        var generator = _serviceProvider.GetRequiredService<IUseCaseGenerator>();
        var useCase = new UseCaseDefinition
        {
            EntityName = entityName,
            Action = action,
            Type = Enum.Parse<UseCaseType>(useCaseType, ignoreCase: true),
            Description = description ?? $"{action} {entityName}"
        };

        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(companyName, projectName),
            OutputPath = string.Empty
        };

        var schema = new DatabaseSchemaEntity();
        return await generator.GenerateHandlerAsync(useCase, schema, config);
    }

    [KernelFunction("generate_result")]
    [Description("Generates a Result/DTO class for a use case")]
    [return: Description("The generated Result class code")]
    public async Task<string> GenerateResultAsync(
        [Description("Entity name")] string entityName,
        [Description("Action name")] string action,
        [Description("Company name for namespace")] string companyName,
        [Description("Project name for namespace")] string projectName,
        [Description("Custom description for prompt context")] string? description = null)
    {
        var generator = _serviceProvider.GetRequiredService<IUseCaseGenerator>();
        var useCase = new UseCaseDefinition
        {
            EntityName = entityName,
            Action = action,
            Type = UseCaseType.Query,
            Description = description ?? $"{action} {entityName}"
        };

        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(companyName, projectName),
            OutputPath = string.Empty
        };

        var schema = new DatabaseSchemaEntity();
        return await generator.GenerateResultAsync(useCase, schema, config);
    }

    [KernelFunction("list_available_actions")]
    [Description("Returns a list of common CRUD actions available for generation")]
    [return: Description("List of action names")]
    public string ListAvailableActions()
    {
        return string.Join(", ", new[]
        {
            "Create", "Update", "Delete",
            "GetById", "GetAll", "GetByFilter",
            "Search", "Activate", "Deactivate",
            "Assign", "Unassign", "Validate"
        });
    }
}
