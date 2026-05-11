namespace Generador.CharmelCodeIA.Domain.Entities;

public class FieldDefinition
{
    public string Name { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string OriginalType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsId { get; set; }
    public bool IsArray { get; set; }
    public bool IsEmbeddedDocument { get; set; }
    public FieldDefinition? ArrayElementType { get; set; }
    public double OccurrenceRate { get; set; } = 1.0;
}
