using Npgsql;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed class PostgreSqlSchemaReader : IDatabaseSchemaReader
{
    private readonly ISqlTypeMapper _mapper = new PostgreSqlTypeMapper();

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == System.Data.ConnectionState.Open;
    }

    public async Task<DatabaseSchemaEntity> ReadSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.Database;
        var tables = await ReadTablesAsync(connection, cancellationToken);
        var relationships = await ReadRelationshipsAsync(connection, cancellationToken);

        return new RelationalSchema
        {
            DatabaseName = databaseName,
            Provider = DatabaseProviderType.PostgreSql,
            Tables = tables,
            Relationships = relationships,
            ReadAt = DateTime.UtcNow
        };
    }

    private async Task<IReadOnlyList<TableDefinition>> ReadTablesAsync(
        NpgsqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT
                c.table_schema,
                c.table_name,
                c.column_name,
                c.data_type,
                c.character_maximum_length,
                c.numeric_precision,
                c.numeric_scale,
                c.is_nullable,
                c.ordinal_position,
                c.column_default,
                CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key,
                CASE WHEN c.is_identity = 'YES' THEN 1 ELSE 0 END AS is_identity
            FROM information_schema.columns c
            INNER JOIN information_schema.tables t
                ON c.table_schema = t.table_schema AND c.table_name = t.table_name
            LEFT JOIN (
                SELECT kcu.table_schema, kcu.table_name, kcu.column_name
                FROM information_schema.table_constraints tc
                INNER JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
            ) pk ON c.table_schema = pk.table_schema
                AND c.table_name = pk.table_name AND c.column_name = pk.column_name
            WHERE t.table_type = 'BASE TABLE'
                AND c.table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY c.table_name, c.ordinal_position
            """;

        using var cmd = new NpgsqlCommand(sql, connection);
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

            var dataType = reader.GetString(3);
            var isNullable = reader.GetString(7) == "YES";

            var column = new ColumnDefinition
            {
                Name = reader.GetString(2),
                SqlType = BuildSqlType(dataType,
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5)),
                    reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6))),
                MaxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Precision = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5)),
                Scale = reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6)),
                IsNullable = isNullable,
                OrdinalPosition = reader.GetInt32(8),
                DefaultValue = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                IsPrimaryKey = reader.GetInt32(10) == 1,
                IsIdentity = reader.GetInt32(11) == 1,
                ClrType = _mapper.MapToClrType(dataType, isNullable)
            };

            ((List<ColumnDefinition>)table.Columns).Add(column);
        }

        return tableDict.Values.ToList();
    }

    private async Task<IReadOnlyList<RelationshipDefinition>> ReadRelationshipsAsync(
        NpgsqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT
                tc.constraint_name,
                kcu.table_schema AS fk_schema,
                kcu.table_name AS fk_table,
                kcu.column_name AS fk_column,
                ccu.table_schema AS pk_schema,
                ccu.table_name AS pk_table,
                ccu.column_name AS pk_column,
                rc.delete_rule
            FROM information_schema.table_constraints tc
            INNER JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
            INNER JOIN information_schema.constraint_column_usage ccu
                ON ccu.constraint_name = tc.constraint_name AND ccu.table_schema = tc.table_schema
            INNER JOIN information_schema.referential_constraints rc
                ON tc.constraint_name = rc.constraint_name AND tc.table_schema = rc.constraint_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
            ORDER BY tc.constraint_name
            """;

        using var cmd = new NpgsqlCommand(sql, connection);
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

    private static string BuildSqlType(string dataType, int? length, int? precision, int? scale)
    {
        var type = dataType.ToUpperInvariant();
        if (length.HasValue && length > 0 && type is "CHAR" or "VARCHAR" or "CHARACTER VARYING")
            return $"{type}({length})";
        if (precision.HasValue)
            return scale.HasValue ? $"{type}({precision},{scale})" : $"{type}({precision})";
        return type;
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
