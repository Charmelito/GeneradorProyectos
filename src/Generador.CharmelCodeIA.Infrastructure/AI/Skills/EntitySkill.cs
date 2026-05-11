using System.ComponentModel;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Generador.CharmelCodeIA.Infrastructure.AI.Skills;

public sealed class EntitySkill
{
    private readonly IServiceProvider _serviceProvider;

    public EntitySkill(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [KernelFunction("generate_entity")]
    [Description("Generates a C# entity class for Entity Framework Core based on a table definition")]
    [return: Description("The generated C# entity code")]
    public async Task<string> GenerateEntityAsync(
        [Description("Table name")] string tableName,
        [Description("JSON representation of ColumnDefinition array")] string columnsJson,
        [Description("Company name for namespace")] string companyName,
        [Description("Project name for namespace")] string projectName,
        [Description("Prompt template ID to use (optional)")] string? promptTemplateId = null)
    {
        var generator = _serviceProvider.GetRequiredService<IEntityGenerator>();
        var table = BuildTableDefinition(tableName, columnsJson);
        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(companyName, projectName),
            OutputPath = string.Empty
        };
        return await generator.GenerateEntityAsync(table, config);
    }

    [KernelFunction("generate_configuration")]
    [Description("Generates an EF Core IEntityTypeConfiguration<T> class for a table")]
    [return: Description("The generated Fluent API configuration code")]
    public async Task<string> GenerateConfigurationAsync(
        [Description("Table name")] string tableName,
        [Description("JSON representation of ColumnDefinition array")] string columnsJson,
        [Description("Company name for namespace")] string companyName,
        [Description("Project name for namespace")] string projectName)
    {
        var generator = _serviceProvider.GetRequiredService<IEntityGenerator>();
        var table = BuildTableDefinition(tableName, columnsJson);
        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(companyName, projectName),
            OutputPath = string.Empty
        };
        return await generator.GenerateConfigurationAsync(table, config);
    }

    [KernelFunction("generate_dbcontext")]
    [Description("Generates an EF Core DbContext class")]
    [return: Description("The generated DbContext code")]
    public async Task<string> GenerateDbContextAsync(
        [Description("JSON array of table names")] string tableNamesJson,
        [Description("Company name for namespace")] string companyName,
        [Description("Project name for namespace")] string projectName)
    {
        var generator = _serviceProvider.GetRequiredService<IEntityGenerator>();
        var tables = System.Text.Json.JsonSerializer.Deserialize<List<TableDefinition>>(tableNamesJson) ?? new();
        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(companyName, projectName),
            OutputPath = string.Empty
        };
        return await generator.GenerateDbContextAsync(tables, config);
    }

    private static TableDefinition BuildTableDefinition(string tableName, string columnsJson)
    {
        var columns = System.Text.Json.JsonSerializer.Deserialize<List<ColumnDefinition>>(columnsJson)
            ?? new List<ColumnDefinition>();

        return new TableDefinition
        {
            Name = tableName,
            Columns = columns
        };
    }
}
