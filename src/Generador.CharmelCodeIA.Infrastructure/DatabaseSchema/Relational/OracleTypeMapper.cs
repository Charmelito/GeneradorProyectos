namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public sealed class OracleTypeMapper : ISqlTypeMapper
{
    public string MapToClrType(string sqlType, bool isNullable)
    {
        var baseType = sqlType.ToUpperInvariant().Trim();

        string clrType;

        if (baseType.StartsWith("NUMBER") || baseType.StartsWith("INTEGER"))
            clrType = DetermineNumericClrType(baseType);
        else if (baseType.StartsWith("VARCHAR2") || baseType.StartsWith("NVARCHAR2"))
            clrType = "string";
        else if (baseType.StartsWith("CHAR") || baseType.StartsWith("NCHAR"))
            clrType = "string";
        else if (baseType is "CLOB" or "NCLOB" or "LONG")
            clrType = "string";
        else if (baseType is "BLOB")
            clrType = "byte[]";
        else if (baseType == "RAW" || baseType.StartsWith("RAW"))
            clrType = "byte[]";
        else if (baseType is "DATE")
            clrType = "DateTime";
        else if (baseType.StartsWith("TIMESTAMP"))
            clrType = "DateTime";
        else if (baseType is "FLOAT" or "BINARY_FLOAT")
            clrType = "float";
        else if (baseType is "BINARY_DOUBLE")
            clrType = "double";
        else if (baseType == "XMLTYPE")
            clrType = "string";
        else
            clrType = "string";

        if (isNullable && IsNullableClrType(clrType))
            return clrType + "?";

        return clrType;
    }

    public int? GetMaxLength(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant();
        if (upper.StartsWith("VARCHAR2") || upper.StartsWith("NVARCHAR2") || upper.StartsWith("CHAR"))
        {
            var start = upper.IndexOf('(');
            var end = upper.IndexOf(')');
            if (start >= 0 && end > start && int.TryParse(upper[(start + 1)..end].Split([' ', 'B', 'C'])[0], out var length))
                return length;
        }
        if (upper is "CLOB" or "NCLOB") return int.MaxValue;
        return null;
    }

    public (int precision, int scale)? GetPrecisionScale(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant();
        if (upper.StartsWith("NUMBER"))
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
            return (38, 0);
        }
        return null;
    }

    public bool IsIdentityType(string sqlType) => false;

    private static string DetermineNumericClrType(string baseType)
    {
        var ps = GetPrecisionScaleStatic(baseType) ?? (38, 0);
        if (ps.scale > 0)
            return "decimal";
        if (ps.precision <= 4) return "short";
        if (ps.precision <= 9) return "int";
        return "long";
    }

    private static (int precision, int scale)? GetPrecisionScaleStatic(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant();
        if (upper.StartsWith("NUMBER"))
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
            return (38, 0);
        }
        return null;
    }

    private static bool IsNullableClrType(string clrType) =>
        clrType is "int" or "long" or "short" or "byte" or "bool" or "double" or "float"
            or "decimal" or "DateTime" or "TimeSpan" or "Guid";
}
