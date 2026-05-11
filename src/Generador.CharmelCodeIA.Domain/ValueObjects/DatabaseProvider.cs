using Generador.CharmelCodeIA.Domain.Enums;

namespace Generador.CharmelCodeIA.Domain.ValueObjects;

public sealed class DatabaseProvider : ValueObject
{
    public DatabaseProviderType Type { get; }
    public string DisplayName { get; }
    public bool IsRelational { get; }
    public bool IsDocument { get; }
    public string DefaultSchema { get; }

    private DatabaseProvider(DatabaseProviderType type, string displayName, bool isRelational, string defaultSchema)
    {
        Type = type;
        DisplayName = displayName;
        IsRelational = isRelational;
        IsDocument = !isRelational;
        DefaultSchema = defaultSchema;
    }

    public static DatabaseProvider SqlServer =>
        new(DatabaseProviderType.SqlServer, "SQL Server", true, "dbo");

    public static DatabaseProvider Oracle =>
        new(DatabaseProviderType.Oracle, "Oracle", true, string.Empty);

    public static DatabaseProvider MySql =>
        new(DatabaseProviderType.MySql, "MySQL", true, string.Empty);

    public static DatabaseProvider PostgreSql =>
        new(DatabaseProviderType.PostgreSql, "PostgreSQL", true, "public");

    public static DatabaseProvider MongoDB =>
        new(DatabaseProviderType.MongoDB, "MongoDB", false, string.Empty);

    public static DatabaseProvider CosmosDB =>
        new(DatabaseProviderType.CosmosDB, "Cosmos DB", false, string.Empty);

    public static DatabaseProvider FromType(DatabaseProviderType type) => type switch
    {
        DatabaseProviderType.SqlServer => SqlServer,
        DatabaseProviderType.Oracle => Oracle,
        DatabaseProviderType.MySql => MySql,
        DatabaseProviderType.PostgreSql => PostgreSql,
        DatabaseProviderType.MongoDB => MongoDB,
        DatabaseProviderType.CosmosDB => CosmosDB,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static IReadOnlyList<DatabaseProvider> All => new[]
    {
        SqlServer, Oracle, MySql, PostgreSql, MongoDB, CosmosDB
    };

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
    }

    public override string ToString() => DisplayName;
}
