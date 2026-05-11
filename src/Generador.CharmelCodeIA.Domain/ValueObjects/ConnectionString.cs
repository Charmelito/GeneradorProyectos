using Generador.CharmelCodeIA.Domain.Enums;

namespace Generador.CharmelCodeIA.Domain.ValueObjects;

public sealed class ConnectionString : ValueObject
{
    public DatabaseProviderType Provider { get; }
    public string Server { get; }
    public int Port { get; }
    public string Database { get; }
    public string User { get; }
    public string Password { get; }

    public ConnectionString(
        DatabaseProviderType provider,
        string server,
        int port,
        string database,
        string user,
        string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        Provider = provider;
        Server = server;
        Port = port > 0 ? port : GetDefaultPort(provider);
        Database = database;
        User = user;
        Password = password;
    }

    public string BuildConnectionString() => Provider switch
    {
        DatabaseProviderType.SqlServer =>
            $"Server={Server},{Port};Database={Database};User Id={User};Password={Password};TrustServerCertificate=True",
        DatabaseProviderType.Oracle =>
            $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={Server})(PORT={Port}))(CONNECT_DATA=(SERVICE_NAME={Database})));User Id={User};Password={Password}",
        DatabaseProviderType.MySql =>
            $"Server={Server};Port={Port};Database={Database};User={User};Password={Password}",
        DatabaseProviderType.PostgreSql =>
            $"Host={Server};Port={Port};Database={Database};Username={User};Password={Password}",
        DatabaseProviderType.MongoDB =>
            $"mongodb://{User}:{Password}@{Server}:{Port}/{Database}?authSource=admin",
        DatabaseProviderType.CosmosDB =>
            $"AccountEndpoint={Server};AccountKey={Password}",
        _ => throw new ArgumentOutOfRangeException(nameof(Provider))
    };

    public string BuildDisplayString() => Provider switch
    {
        DatabaseProviderType.SqlServer => $"[{Provider}] {Database}@{Server}:{Port}",
        DatabaseProviderType.Oracle => $"[{Provider}] {Database}@{Server}:{Port}/{Database}",
        DatabaseProviderType.MySql => $"[{Provider}] {Database}@{Server}:{Port}",
        DatabaseProviderType.PostgreSql => $"[{Provider}] {Database}@{Server}:{Port}",
        DatabaseProviderType.MongoDB => $"[{Provider}] {Database}@{Server}:{Port}",
        DatabaseProviderType.CosmosDB => $"[{Provider}] {Database}@{Server}",
        _ => $"[{Provider}] {Database}@{Server}"
    };

    private static int GetDefaultPort(DatabaseProviderType provider) => provider switch
    {
        DatabaseProviderType.SqlServer => 1433,
        DatabaseProviderType.Oracle => 1521,
        DatabaseProviderType.MySql => 3306,
        DatabaseProviderType.PostgreSql => 5432,
        DatabaseProviderType.MongoDB => 27017,
        DatabaseProviderType.CosmosDB => 443,
        _ => 0
    };

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Provider;
        yield return Server.ToLowerInvariant();
        yield return Port;
        yield return Database.ToLowerInvariant();
        yield return User.ToLowerInvariant();
    }
}
