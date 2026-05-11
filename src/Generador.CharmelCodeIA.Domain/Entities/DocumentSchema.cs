namespace Generador.CharmelCodeIA.Domain.Entities;

public class DocumentSchema : DatabaseSchema
{
    public IReadOnlyList<DocumentDefinition> Collections { get; set; } = Array.Empty<DocumentDefinition>();
    public int SampleSize { get; set; }
}

public class DocumentDefinition
{
    public string CollectionName { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    public IReadOnlyList<FieldDefinition> Fields { get; set; } = Array.Empty<FieldDefinition>();
    public IReadOnlyList<EmbeddedDocumentDefinition> EmbeddedDocuments { get; set; } = Array.Empty<EmbeddedDocumentDefinition>();
    public IReadOnlyList<ArrayRelationDefinition> ArrayRelations { get; set; } = Array.Empty<ArrayRelationDefinition>();
}

public class EmbeddedDocumentDefinition
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<FieldDefinition> Fields { get; set; } = Array.Empty<FieldDefinition>();
    public bool IsArray { get; set; }
}

public class ArrayRelationDefinition
{
    public string FieldName { get; set; } = string.Empty;
    public string ReferencedCollection { get; set; } = string.Empty;
    public string ForeignField { get; set; } = string.Empty;
}
