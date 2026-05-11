using System.Text.RegularExpressions;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed partial class MySqlTypeMapper : ISqlTypeMapper
{
    public string MapToClrType(string sqlType, bool isNullable)
    {
        var baseType = sqlType.ToUpperInvariant().Trim();

        string clrType;

        if (baseType.StartsWith("TINYINT(1)") || baseType.StartsWith("BIT"))
            clrType = "bool";
        else if (baseType.StartsWith("TINYINT"))
            clrType = "byte";
        else if (baseType.StartsWith("SMALLINT"))
            clrType = "short";
        else if (baseType.StartsWith("MEDIUMINT") || baseType.StartsWith("INT"))
            clrType = "int";
        else if (baseType.StartsWith("BIGINT"))
            clrType = "long";
        else if (baseType.StartsWith("FLOAT"))
            clrType = "float";
        else if (baseType.StartsWith("DOUBLE"))
            clrType = "double";
        else if (baseType.StartsWith("DECIMAL") || baseType.StartsWith("NUMERIC"))
            clrType = "decimal";
        else if (baseType.StartsWith("VARCHAR") || baseType.StartsWith("CHAR"))
            clrType = "string";
        else if (baseType is "TINYTEXT" or "TEXT" or "MEDIUMTEXT" or "LONGTEXT" or "JSON")
            clrType = "string";
        else if (baseType is "DATE" or "DATETIME" or "TIMESTAMP")
            clrType = "DateTime";
        else if (baseType == "TIME")
            clrType = "TimeSpan";
        else if (baseType == "YEAR")
            clrType = "int";
        else if (baseType.StartsWith("BINARY") || baseType.StartsWith("VARBINARY"))
            clrType = "byte[]";
        else if (baseType is "TINYBLOB" or "BLOB" or "MEDIUMBLOB" or "LONGBLOB")
            clrType = "byte[]";
        else if (baseType is "ENUM" or "SET")
            clrType = "string";
        else if (baseType is "UUID" or "GUID")
            clrType = "Guid";
        else
            clrType = "string";

        if (isNullable && IsNullableClrType(clrType))
            return clrType + "?";

        return clrType;
    }

    public int? GetMaxLength(string sqlType)
    {
        var match = MaxLengthRegex().Match(sqlType.ToUpperInvariant());
        if (match.Success && int.TryParse(match.Groups[1].Value, out var length))
            return length;
        return null;
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
        sqlType.ToUpperInvariant() is "INT" or "BIGINT" or "SMALLINT" or "MEDIUMINT" or "TINYINT";

    private static bool IsNullableClrType(string clrType) =>
        clrType is "int" or "long" or "short" or "byte" or "bool" or "double" or "float"
            or "decimal" or "DateTime" or "TimeSpan" or "Guid";

    [GeneratedRegex(@"\((\d+)\)", RegexOptions.Compiled)]
    private static partial Regex MaxLengthRegex();

    [GeneratedRegex(@"\((\d+)(?:,\s*(\d+))?\)", RegexOptions.Compiled)]
    private static partial Regex PrecisionScaleRegex();
}
