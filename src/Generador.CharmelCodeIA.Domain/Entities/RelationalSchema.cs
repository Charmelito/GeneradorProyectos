namespace Generador.CharmelCodeIA.Domain.Entities;

public class RelationalSchema : DatabaseSchema
{
    public IReadOnlyList<IndexDefinition> Indexes { get; set; } = Array.Empty<IndexDefinition>();
    public IReadOnlyList<StoredProcedureDefinition> StoredProcedures { get; set; } = Array.Empty<StoredProcedureDefinition>();
    public IReadOnlyList<ViewDefinition> Views { get; set; } = Array.Empty<ViewDefinition>();
}

public class IndexDefinition
{
    public string TableName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public bool IsClustered { get; set; }
    public IReadOnlyList<string> Columns { get; set; } = Array.Empty<string>();
}

public class StoredProcedureDefinition
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}

public class ViewDefinition
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public IReadOnlyList<ColumnDefinition> Columns { get; set; } = Array.Empty<ColumnDefinition>();
}
