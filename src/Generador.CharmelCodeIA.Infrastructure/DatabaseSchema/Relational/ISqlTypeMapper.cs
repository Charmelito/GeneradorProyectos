namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.Relational;

public interface ISqlTypeMapper
{
    string MapToClrType(string sqlType, bool isNullable);
    int? GetMaxLength(string sqlType);
    (int precision, int scale)? GetPrecisionScale(string sqlType);
    bool IsIdentityType(string sqlType);
}
