using System.ComponentModel;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.AI.Skills;

public sealed class DatabaseSkill
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSkill(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [KernelFunction("read_schema")]
    [Description("Reads the complete database schema from the specified connection string")]
    [return: Description("The database schema with tables, columns, and relationships")]
    public async Task<DatabaseSchemaEntity> ReadSchemaAsync(
        [Description("Full connection string to the database")]
        string connectionString,
        [Description("Database provider type: SqlServer, Oracle, MySql, PostgreSql, MongoDB, CosmosDB")]
        string providerType)
    {
        var factory = _serviceProvider.GetRequiredService<ISchemaReaderFactory>();
        var provider = Enum.Parse<DatabaseProviderType>(providerType, ignoreCase: true);
        var reader = factory.Create(provider);
        return await reader.ReadSchemaAsync(connectionString);
    }

    [KernelFunction("test_connection")]
    [Description("Tests if the database connection is valid")]
    [return: Description("True if connection succeeded")]
    public async Task<bool> TestConnectionAsync(
        [Description("Full connection string to the database")]
        string connectionString,
        [Description("Database provider type: SqlServer, Oracle, MySql, PostgreSql, MongoDB, CosmosDB")]
        string providerType)
    {
        var factory = _serviceProvider.GetRequiredService<ISchemaReaderFactory>();
        var provider = Enum.Parse<DatabaseProviderType>(providerType, ignoreCase: true);
        var reader = factory.Create(provider);
        return await reader.TestConnectionAsync(connectionString);
    }

    [KernelFunction("get_tables_summary")]
    [Description("Returns a summary of all tables in the schema")]
    [return: Description("Summary text listing tables and their column counts")]
    public string GetTablesSummary(DatabaseSchemaEntity schema)
    {
        var lines = new List<string> { $"Database: {schema.DatabaseName} ({schema.Provider})" };
        foreach (var table in schema.Tables)
        {
            var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);
            lines.Add($"  {table.Name}: {table.Columns.Count} columns, PK={pk?.Name ?? "none"}");
        }
        return string.Join("\n", lines);
    }
}
