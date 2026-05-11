namespace Generador.CharmelCodeIA.Domain.Entities;

public class TableDefinition
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<ColumnDefinition> Columns { get; set; } = Array.Empty<ColumnDefinition>();
    public IReadOnlyList<IndexDefinition> Indexes { get; set; } = Array.Empty<IndexDefinition>();
    public string Comment { get; set; } = string.Empty;
}
