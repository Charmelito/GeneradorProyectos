using Microsoft.Data.SqlClient;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed class SqlServerSchemaReader : IDatabaseSchemaReader
{
    private readonly ISqlTypeMapper _mapper = new SqlServerTypeMapper();

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == System.Data.ConnectionState.Open;
    }

    public async Task<DatabaseSchemaEntity> ReadSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.Database;
        var tables = await ReadTablesAsync(connection, cancellationToken);
        var relationships = await ReadRelationshipsAsync(connection, cancellationToken);

        return new RelationalSchema
        {
            DatabaseName = databaseName,
            Provider = DatabaseProviderType.SqlServer,
            Tables = tables,
            Relationships = relationships,
            Indexes = await ReadIndexesAsync(connection, cancellationToken),
            ReadAt = DateTime.UtcNow
        };
    }

    private async Task<IReadOnlyList<TableDefinition>> ReadTablesAsync(
        SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT
                t.TABLE_SCHEMA,
                t.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.NUMERIC_PRECISION,
                c.NUMERIC_SCALE,
                c.IS_NULLABLE,
                c.ORDINAL_POSITION,
                c.COLUMN_DEFAULT,
                ISNULL(col.is_identity, 0) AS IS_IDENTITY,
                ISNULL(col.is_computed, 0) AS IS_COMPUTED,
                ISNULL(pk.is_primary_key, 0) AS IS_PRIMARY_KEY,
                ISNULL(fk.constraint_name, '') AS FK_CONSTRAINT
            FROM INFORMATION_SCHEMA.TABLES t
            INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
            OUTER APPLY (
                SELECT TOP 1 1 AS is_identity
                FROM sys.columns sc INNER JOIN sys.tables st ON sc.object_id = st.object_id
                WHERE st.name = t.TABLE_NAME AND sc.name = c.COLUMN_NAME AND sc.is_identity = 1
            ) col
            OUTER APPLY (
                SELECT TOP 1 1 AS is_computed
                FROM sys.columns sc INNER JOIN sys.tables st ON sc.object_id = st.object_id
                WHERE st.name = t.TABLE_NAME AND sc.name = c.COLUMN_NAME AND sc.is_computed = 1
            ) col_comp
            OUTER APPLY (
                SELECT TOP 1 1 AS is_primary_key
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    AND tc.TABLE_NAME = t.TABLE_NAME AND kcu.COLUMN_NAME = c.COLUMN_NAME
            ) pk
            OUTER APPLY (
                SELECT TOP 1 tc.CONSTRAINT_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
                    AND kcu.TABLE_NAME = t.TABLE_NAME AND kcu.COLUMN_NAME = c.COLUMN_NAME
            ) fk
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION
            """;

        using var cmd = new SqlCommand(sql, connection);
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

            var column = new ColumnDefinition
            {
                Name = reader.GetString(2),
                SqlType = BuildSqlType(reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    reader.IsDBNull(6) ? null : reader.GetInt32(6)),
                MaxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Precision = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Scale = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                IsNullable = reader.GetString(7) == "YES",
                OrdinalPosition = reader.GetInt32(8),
                DefaultValue = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                IsIdentity = reader.GetInt32(10) == 1,
                IsComputed = reader.GetInt32(11) == 1,
                IsPrimaryKey = reader.GetInt32(12) == 1,
                IsForeignKey = !reader.IsDBNull(13) && reader.GetString(13).Length > 0,
                ClrType = _mapper.MapToClrType(reader.GetString(3), reader.GetString(7) == "YES")
            };

            ((List<ColumnDefinition>)table.Columns).Add(column);
        }

        return tableDict.Values.ToList();
    }

    private async Task<IReadOnlyList<RelationshipDefinition>> ReadRelationshipsAsync(
        SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT
                fk.name AS FK_NAME,
                OBJECT_SCHEMA_NAME(fk.parent_object_id) AS FK_SCHEMA,
                OBJECT_NAME(fk.parent_object_id) AS FK_TABLE,
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS FK_COLUMN,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS PK_SCHEMA,
                OBJECT_NAME(fk.referenced_object_id) AS PK_TABLE,
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS PK_COLUMN,
                fk.delete_referential_action,
                fk.is_disabled
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            WHERE fk.is_disabled = 0
            ORDER BY fk.name
            """;

        using var cmd = new SqlCommand(sql, connection);
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
                DeleteBehavior = MapDeleteBehavior(reader.GetByte(7)),
                Type = RelationshipType.OneToMany,
                IsRequired = false
            });
        }

        return relationships;
    }

    private async Task<IReadOnlyList<IndexDefinition>> ReadIndexesAsync(
        SqlConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT
                OBJECT_NAME(i.object_id) AS TableName,
                i.name AS IndexName,
                i.is_unique,
                i.type,
                STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal) AS Columns
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE i.type > 0 AND i.is_primary_key = 0
            GROUP BY OBJECT_NAME(i.object_id), i.name, i.is_unique, i.type
            """;

        using var cmd = new SqlCommand(sql, connection);
        using var reader = await cmd.ExecuteReaderAsync(ct);

        var indexes = new List<IndexDefinition>();
        while (await reader.ReadAsync(ct))
        {
            indexes.Add(new IndexDefinition
            {
                TableName = reader.GetString(0),
                Name = reader.GetString(1),
                IsUnique = reader.GetBoolean(2),
                IsClustered = reader.GetByte(3) == 1,
                Columns = reader.GetString(4).Split(',')
            });
        }

        return indexes;
    }

    private static string BuildSqlType(string dataType, int? maxLength, int? precision, int? scale)
    {
        var type = dataType.ToUpperInvariant();
        if (maxLength.HasValue && maxLength > 0) return $"{type}({maxLength})";
        if (precision.HasValue && scale.HasValue) return $"{type}({precision},{scale})";
        return type;
    }

    private static DeleteBehavior MapDeleteBehavior(byte action) => action switch
    {
        0 => DeleteBehavior.NoAction,
        1 => DeleteBehavior.Cascade,
        2 => DeleteBehavior.SetNull,
        3 => DeleteBehavior.Restrict,
        _ => DeleteBehavior.NoAction
    };
}
