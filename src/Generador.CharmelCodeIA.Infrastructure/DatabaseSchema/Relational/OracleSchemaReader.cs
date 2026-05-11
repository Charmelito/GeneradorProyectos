using Oracle.ManagedDataAccess.Client;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed class OracleSchemaReader : IDatabaseSchemaReader
{
    private readonly ISqlTypeMapper _mapper = new OracleTypeMapper();

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection.State == System.Data.ConnectionState.Open;
    }

    public async Task<DatabaseSchemaEntity> ReadSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.DatabaseName;
        var tables = await ReadTablesAsync(connection, cancellationToken);
        var relationships = await ReadRelationshipsAsync(connection, cancellationToken);

        return new RelationalSchema
        {
            DatabaseName = databaseName,
            Provider = DatabaseProviderType.Oracle,
            Tables = tables,
            Relationships = relationships,
            ReadAt = DateTime.UtcNow
        };
    }

    private async Task<IReadOnlyList<TableDefinition>> ReadTablesAsync(
        OracleConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT
                t.OWNER AS TABLE_SCHEMA,
                t.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHAR_LENGTH,
                c.DATA_PRECISION,
                c.DATA_SCALE,
                c.NULLABLE,
                c.COLUMN_ID,
                c.DATA_DEFAULT,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY,
                CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_FOREIGN_KEY,
                CASE WHEN c.IDENTITY_COLUMN = 'YES' THEN 1 ELSE 0 END AS IS_IDENTITY
            FROM ALL_TABLES t
            INNER JOIN ALL_TAB_COLUMNS c ON t.OWNER = c.OWNER AND t.TABLE_NAME = c.TABLE_NAME
            LEFT JOIN (
                SELECT cc.OWNER, cc.TABLE_NAME, cc.COLUMN_NAME
                FROM ALL_CONS_COLUMNS cc
                INNER JOIN ALL_CONSTRAINTS ac ON cc.CONSTRAINT_NAME = ac.CONSTRAINT_NAME AND cc.OWNER = ac.OWNER
                WHERE ac.CONSTRAINT_TYPE = 'P'
            ) pk ON c.OWNER = pk.OWNER AND c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
            LEFT JOIN (
                SELECT cc.OWNER, cc.TABLE_NAME, cc.COLUMN_NAME
                FROM ALL_CONS_COLUMNS cc
                INNER JOIN ALL_CONSTRAINTS ac ON cc.CONSTRAINT_NAME = ac.CONSTRAINT_NAME AND cc.OWNER = ac.OWNER
                WHERE ac.CONSTRAINT_TYPE = 'R'
            ) fk ON c.OWNER = fk.OWNER AND c.TABLE_NAME = fk.TABLE_NAME AND c.COLUMN_NAME = fk.COLUMN_NAME
            ORDER BY t.TABLE_NAME, c.COLUMN_ID
            """;

        using var cmd = new OracleCommand(sql, connection);
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

            var sqlType = reader.GetString(3);
            var column = new ColumnDefinition
            {
                Name = reader.GetString(2),
                SqlType = BuildSqlType(sqlType, ReadNullableInt(reader, 4), ReadNullableInt(reader, 5), ReadNullableInt(reader, 6)),
                MaxLength = ReadNullableInt(reader, 4),
                Precision = ReadNullableInt(reader, 5),
                Scale = ReadNullableInt(reader, 6),
                IsNullable = reader.GetString(7) == "Y",
                OrdinalPosition = reader.GetInt32(8),
                DefaultValue = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                IsPrimaryKey = reader.GetInt32(10) == 1,
                IsForeignKey = reader.GetInt32(11) == 1,
                IsIdentity = reader.GetInt32(12) == 1,
                ClrType = _mapper.MapToClrType(sqlType, reader.GetString(7) == "Y")
            };

            ((List<ColumnDefinition>)table.Columns).Add(column);
        }

        return tableDict.Values.ToList();
    }

    private async Task<IReadOnlyList<RelationshipDefinition>> ReadRelationshipsAsync(
        OracleConnection connection, CancellationToken ct)
    {
        const string sql = """
            SELECT
                ac.CONSTRAINT_NAME,
                acc.OWNER AS FK_SCHEMA,
                acc.TABLE_NAME AS FK_TABLE,
                acc.COLUMN_NAME AS FK_COLUMN,
                acc_pk.OWNER AS PK_SCHEMA,
                acc_pk.TABLE_NAME AS PK_TABLE,
                acc_pk.COLUMN_NAME AS PK_COLUMN,
                ac.DELETE_RULE
            FROM ALL_CONSTRAINTS ac
            INNER JOIN ALL_CONS_COLUMNS acc ON ac.CONSTRAINT_NAME = acc.CONSTRAINT_NAME AND ac.OWNER = acc.OWNER
            INNER JOIN ALL_CONS_COLUMNS acc_pk ON ac.R_CONSTRAINT_NAME = acc_pk.CONSTRAINT_NAME
                AND ac.R_OWNER = acc_pk.OWNER AND acc.POSITION = acc_pk.POSITION
            WHERE ac.CONSTRAINT_TYPE = 'R'
            ORDER BY ac.CONSTRAINT_NAME
            """;

        using var cmd = new OracleCommand(sql, connection);
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
        if (length.HasValue && length > 0) return $"{type}({length})";
        if (precision.HasValue && scale.HasValue) return $"{type}({precision},{scale})";
        return type;
    }

    private static int? ReadNullableInt(OracleDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static DeleteBehavior MapDeleteBehavior(string rule) => rule?.ToUpperInvariant() switch
    {
        "CASCADE" => DeleteBehavior.Cascade,
        "SET NULL" => DeleteBehavior.SetNull,
        "NO ACTION" => DeleteBehavior.NoAction,
        _ => DeleteBehavior.NoAction
    };
}
