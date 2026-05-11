using MySql.Data.MySqlClient;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed class MySqlSchemaReader : IDatabaseSchemaReader
{
    private readonly ISqlTypeMapper _mapper = new MySqlTypeMapper();

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == System.Data.ConnectionState.Open;
    }

    public async Task<DatabaseSchemaEntity> ReadSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.Database;
        var tables = await ReadTablesAsync(connection, databaseName, cancellationToken);
        var relationships = await ReadRelationshipsAsync(connection, databaseName, cancellationToken);

        return new RelationalSchema
        {
            DatabaseName = databaseName,
            Provider = DatabaseProviderType.MySql,
            Tables = tables,
            Relationships = relationships,
            ReadAt = DateTime.UtcNow
        };
    }

    private async Task<IReadOnlyList<TableDefinition>> ReadTablesAsync(
        MySqlConnection connection, string databaseName, CancellationToken ct)
    {
        const string sql = """
            SELECT
                c.TABLE_SCHEMA,
                c.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.NUMERIC_PRECISION,
                c.NUMERIC_SCALE,
                c.IS_NULLABLE,
                c.ORDINAL_POSITION,
                c.COLUMN_DEFAULT,
                c.EXTRA,
                c.COLUMN_TYPE,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
            FROM INFORMATION_SCHEMA.COLUMNS c
            INNER JOIN INFORMATION_SCHEMA.TABLES t
                ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
            LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk
                ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                AND c.TABLE_NAME = pk.TABLE_NAME
                AND c.COLUMN_NAME = pk.COLUMN_NAME
                AND pk.CONSTRAINT_NAME = 'PRIMARY'
            WHERE c.TABLE_SCHEMA = @database AND t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION
            """;

        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@database", databaseName);
        using var reader = await cmd.ExecuteReaderAsync(ct);

        var tableDict = new Dictionary<string, TableDefinition>();

        while (await reader.ReadAsync(ct))
        {
            var tableName = reader.GetString(1);
            if (!tableDict.TryGetValue(tableName, out var table))
            {
                table = new TableDefinition
                {
                    Schema = reader.GetString(0),
                    Name = tableName,
                    Columns = new List<ColumnDefinition>()
                };
                tableDict[tableName] = table;
            }

            var extra = reader.IsDBNull(10) ? string.Empty : reader.GetString(10);
            var columnType = reader.IsDBNull(11) ? reader.GetString(3) : reader.GetString(11);

            var column = new ColumnDefinition
            {
                Name = reader.GetString(2),
                SqlType = columnType,
                MaxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Precision = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5)),
                Scale = reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6)),
                IsNullable = reader.GetString(7) == "YES",
                OrdinalPosition = reader.GetInt32(8),
                DefaultValue = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                IsPrimaryKey = reader.GetInt32(12) == 1,
                IsIdentity = extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                ClrType = _mapper.MapToClrType(columnType, reader.GetString(7) == "YES")
            };

            ((List<ColumnDefinition>)table.Columns).Add(column);
        }

        return tableDict.Values.ToList();
    }

    private async Task<IReadOnlyList<RelationshipDefinition>> ReadRelationshipsAsync(
        MySqlConnection connection, string databaseName, CancellationToken ct)
    {
        const string sql = """
            SELECT
                kcu.CONSTRAINT_NAME,
                kcu.TABLE_SCHEMA AS FK_SCHEMA,
                kcu.TABLE_NAME AS FK_TABLE,
                kcu.COLUMN_NAME AS FK_COLUMN,
                kcu.REFERENCED_TABLE_SCHEMA AS PK_SCHEMA,
                kcu.REFERENCED_TABLE_NAME AS PK_TABLE,
                kcu.REFERENCED_COLUMN_NAME AS PK_COLUMN,
                rc.DELETE_RULE
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                ON kcu.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA
                AND kcu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
            WHERE kcu.REFERENCED_TABLE_NAME IS NOT NULL AND kcu.TABLE_SCHEMA = @database
            ORDER BY kcu.CONSTRAINT_NAME
            """;

        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@database", databaseName);
        using var reader = await cmd.ExecuteReaderAsync(ct);

        var relationships = new List<RelationshipDefinition>();
        while (await reader.ReadAsync(ct))
        {
            relationships.Add(new RelationshipDefinition
            {
                Name = reader.GetString(0),
                DependentSchema = reader.GetString(1),
                DependentTable = reader.GetString(2),
                DependentColumn = reader.GetString(3),
                PrincipalSchema = reader.GetString(4),
                PrincipalTable = reader.GetString(5),
                PrincipalColumn = reader.GetString(6),
                DeleteBehavior = MapDeleteBehavior(reader.GetString(7)),
                Type = RelationshipType.OneToMany,
                IsRequired = false
            });
        }

        return relationships;
    }

    private static DeleteBehavior MapDeleteBehavior(string rule) => rule?.ToUpperInvariant() switch
    {
        "CASCADE" => DeleteBehavior.Cascade,
        "SET NULL" => DeleteBehavior.SetNull,
        "NO ACTION" => DeleteBehavior.NoAction,
        "RESTRICT" => DeleteBehavior.Restrict,
        _ => DeleteBehavior.NoAction
    };
}
