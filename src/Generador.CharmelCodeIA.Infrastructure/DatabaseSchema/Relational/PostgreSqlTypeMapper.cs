namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed class PostgreSqlTypeMapper : ISqlTypeMapper
{
    public string MapToClrType(string sqlType, bool isNullable)
    {
        var baseType = sqlType.ToUpperInvariant().Trim();

        string clrType;

        if (baseType is "BOOLEAN" or "BOOL")
            clrType = "bool";
        else if (baseType is "SMALLINT" or "INT2")
            clrType = "short";
        else if (baseType is "INTEGER" or "INT" or "INT4")
            clrType = "int";
        else if (baseType is "BIGINT" or "INT8")
            clrType = "long";
        else if (baseType is "REAL" or "FLOAT4")
            clrType = "float";
        else if (baseType is "DOUBLE PRECISION" or "FLOAT8")
            clrType = "double";
        else if (baseType.StartsWith("NUMERIC") || baseType.StartsWith("DECIMAL"))
            clrType = "decimal";
        else if (baseType.StartsWith("MONEY"))
            clrType = "decimal";
        else if (baseType.StartsWith("VARCHAR") || baseType.StartsWith("CHARACTER VARYING"))
            clrType = "string";
        else if (baseType.StartsWith("CHAR") || baseType.StartsWith("BPCHAR"))
            clrType = "string";
        else if (baseType is "TEXT" or "CITEXT")
            clrType = "string";
        else if (baseType == "UUID")
            clrType = "Guid";
        else if (baseType == "DATE")
            clrType = "DateTime";
        else if (baseType is "TIME" or "TIMETZ")
            clrType = "TimeSpan";
        else if (baseType.StartsWith("TIMESTAMP") || baseType.StartsWith("TIMESTAMPTZ"))
            clrType = "DateTime";
        else if (baseType == "INTERVAL")
            clrType = "TimeSpan";
        else if (baseType.StartsWith("BYTEA"))
            clrType = "byte[]";
        else if (baseType is "JSON" or "JSONB" or "XML")
            clrType = "string";
        else if (baseType is "INET" or "CIDR" or "MACADDR")
            clrType = "string";
        else if (baseType == "OID")
            clrType = "uint";
        else if (baseType is "SERIAL")
            clrType = "int";
        else if (baseType is "BIGSERIAL")
            clrType = "long";
        else if (baseType is "SMALLSERIAL")
            clrType = "short";
        else
            clrType = "string";

        if (isNullable && IsNullableClrType(clrType))
            return clrType + "?";

        return clrType;
    }

    public int? GetMaxLength(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant();
        var start = upper.IndexOf('(');
        var end = upper.IndexOf(')');
        if (start >= 0 && end > start && int.TryParse(upper[(start + 1)..end], out var length))
            return length;
        return null;
    }

    public (int precision, int scale)? GetPrecisionScale(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant();
        if (upper.StartsWith("NUMERIC") || upper.StartsWith("DECIMAL"))
        {
            var start = upper.IndexOf('(');
            var end = upper.IndexOf(')');
            if (start >= 0 && end > start)
            {
                var parts = upper[(start + 1)..end].Split(',');
                var precision = int.Parse(parts[0].Trim());
                var scale = parts.Length > 1 ? int.Parse(parts[1].Trim()) : 0;
                return (precision, scale);
            }
        }
        return null;
    }

    public bool IsIdentityType(string sqlType) =>
        sqlType.ToUpperInvariant() is "SERIAL" or "BIGSERIAL" or "SMALLSERIAL";

    private static bool IsNullableClrType(string clrType) =>
        clrType is "int" or "long" or "short" or "byte" or "bool" or "double" or "float"
            or "decimal" or "DateTime" or "TimeSpan" or "Guid";
}
