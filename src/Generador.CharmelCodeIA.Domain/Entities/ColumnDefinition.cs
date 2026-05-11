namespace Generador.CharmelCodeIA.Domain.Entities;

public class ColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string SqlType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int OrdinalPosition { get; set; }
}
