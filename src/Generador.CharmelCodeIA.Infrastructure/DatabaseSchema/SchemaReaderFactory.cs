using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.NoSql;
using Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;
using Microsoft.Extensions.DependencyInjection;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema;

public sealed class SchemaReaderFactory : ISchemaReaderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SchemaReaderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDatabaseSchemaReader Create(DatabaseProviderType providerType) => providerType switch
    {
        DatabaseProviderType.SqlServer => new SqlServerSchemaReader(),
        DatabaseProviderType.Oracle => new OracleSchemaReader(),
        DatabaseProviderType.MySql => new MySqlSchemaReader(),
        DatabaseProviderType.PostgreSql => new PostgreSqlSchemaReader(),
        DatabaseProviderType.MongoDB => new MongoSchemaReader(
            _serviceProvider.GetRequiredService<ISchemaInferenceStrategy>()),
        DatabaseProviderType.CosmosDB => new CosmosSchemaReader(
            _serviceProvider.GetRequiredService<ISchemaInferenceStrategy>()),
        _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
    };
}
