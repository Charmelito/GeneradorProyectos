using System.Text.RegularExpressions;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed partial class SqlServerTypeMapper : ISqlTypeMapper
{
    public string MapToClrType(string sqlType, bool isNullable)
    {
        var baseType = sqlType.ToUpperInvariant().Trim();

        var clrType = baseType switch
        {
            "BIGINT" => "long",
            "BIT" => "bool",
            "DATE" or "DATETIME" or "DATETIME2" or "SMALLDATETIME" or "DATETIMEOFFSET" => "DateTime",
            "DECIMAL" or "NUMERIC" or "MONEY" or "SMALLMONEY" => "decimal",
            "FLOAT" => "double",
            "IMAGE" => "byte[]",
            "INT" => "int",
            "REAL" => "float",
            "SMALLINT" => "short",
            "TEXT" or "NTEXT" => "string",
            "TIME" => "TimeSpan",
            "TINYINT" => "byte",
            "UNIQUEIDENTIFIER" => "Guid",
            "VARBINARY" or "BINARY" or "ROWVERSION" or "TIMESTAMP" => "byte[]",
            "XML" => "string",
            "GEOGRAPHY" or "GEOMETRY" => "object",
            _ when baseType.StartsWith("NVARCHAR") || baseType.StartsWith("NCHAR") || baseType == "NTEXT" => "string",
            _ when baseType.StartsWith("VARCHAR") || baseType.StartsWith("CHAR") => "string",
            _ when baseType.StartsWith("VARBINARY") || baseType.StartsWith("BINARY") => "byte[]",
            _ => "string"
        };

        if (isNullable && IsNullableClrType(clrType))
            return clrType + "?";

        return clrType;
    }

    public int? GetMaxLength(string sqlType)
    {
        var match = MaxLengthRegex().Match(sqlType.ToUpperInvariant());
        if (match.Success && int.TryParse(match.Groups[1].Value, out var length))
            return length == -1 ? null : length;

        return sqlType.ToUpperInvariant() switch
        {
            "NVARCHAR" or "VARCHAR" or "NCHAR" or "CHAR" => null,
            _ => null
        };
    }

    public (int precision, int scale)? GetPrecisionScale(string sqlType)
    {
        var match = PrecisionScaleRegex().Match(sqlType.ToUpperInvariant());
        if (match.Success && int.TryParse(match.Groups[1].Value, out var precision))
        {
            var scale = match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out var s) ? s : 0;
            return (precision, scale);
        }
        return null;
    }

    public bool IsIdentityType(string sqlType) =>
        sqlType.ToUpperInvariant() is "INT" or "BIGINT" or "SMALLINT" or "TINYINT";

    private static bool IsNullableClrType(string clrType) =>
        clrType is "int" or "long" or "short" or "byte" or "bool" or "double" or "float"
            or "decimal" or "DateTime" or "TimeSpan" or "Guid";

    [GeneratedRegex(@"\((\d+|-1)\)", RegexOptions.Compiled)]
    private static partial Regex MaxLengthRegex();

    [GeneratedRegex(@"\((\d+)(?:,\s*(\d+))?\)", RegexOptions.Compiled)]
    private static partial Regex PrecisionScaleRegex();
}
