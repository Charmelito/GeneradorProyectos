using Generador.CharmelCodeIA.Domain.Enums;

namespace Generador.CharmelCodeIA.Domain.Entities;

public class DatabaseSchema
{
    public string DatabaseName { get; set; } = string.Empty;
    public DatabaseProviderType Provider { get; set; }
    public IReadOnlyList<TableDefinition> Tables { get; set; } = Array.Empty<TableDefinition>();
    public IReadOnlyList<RelationshipDefinition> Relationships { get; set; } = Array.Empty<RelationshipDefinition>();
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
