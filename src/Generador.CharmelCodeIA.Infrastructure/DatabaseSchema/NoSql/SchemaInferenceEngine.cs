using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.NoSql;

public sealed class SchemaInferenceEngine : ISchemaInferenceStrategy
{
    public DocumentDefinition InferSchema(string collectionName, IReadOnlyList<Dictionary<string, object>> sampleDocuments)
    {
        if (sampleDocuments.Count == 0)
        {
            return new DocumentDefinition
            {
                CollectionName = collectionName,
                Fields = Array.Empty<FieldDefinition>()
            };
        }

        var mergedFields = new Dictionary<string, FieldStats>();

        foreach (var doc in sampleDocuments)
        {
            foreach (var (fieldName, value) in doc)
            {
                if (!mergedFields.ContainsKey(fieldName))
                    mergedFields[fieldName] = new FieldStats();

                mergedFields[fieldName].Update(value);
            }
        }

        var fields = mergedFields.Select(kvp =>
        {
            var stats = kvp.Value;
            var inferredType = DetermineClrType(stats);
            var isArray = stats.TypeCounts.Keys.Any(t => t == "List`1" || t == "Array");

            return new FieldDefinition
            {
                Name = kvp.Key,
                ClrType = inferredType,
                OriginalType = stats.PrimaryOriginalType ?? "object",
                IsRequired = stats.OccurrenceRate >= 0.95,
                IsId = IsIdField(kvp.Key),
                IsArray = isArray,
                IsEmbeddedDocument = stats.TypeCounts.ContainsKey("Dictionary"),
                OccurrenceRate = stats.OccurrenceRate
            };
        }).ToList();

        var definition = new DocumentDefinition
        {
            CollectionName = collectionName,
            Fields = fields
        };

        definition.EmbeddedDocuments = DetectEmbeddedDocuments(definition);
        var arrayRelations = DetectArrayRelations(definition, Array.Empty<string>());

        return definition;
    }

    public FieldDefinition InferFieldType(string fieldName, IReadOnlyList<object> sampleValues)
    {
        var stats = new FieldStats();
        foreach (var value in sampleValues)
            stats.Update(value);

        return new FieldDefinition
        {
            Name = fieldName,
            ClrType = DetermineClrType(stats),
            OriginalType = stats.PrimaryOriginalType ?? "object",
            IsRequired = stats.OccurrenceRate >= 0.95,
            IsId = IsIdField(fieldName),
            OccurrenceRate = stats.OccurrenceRate
        };
    }

    public IReadOnlyList<EmbeddedDocumentDefinition> DetectEmbeddedDocuments(DocumentDefinition document)
    {
        var embeddedDocs = new List<EmbeddedDocumentDefinition>();

        foreach (var field in document.Fields.Where(f => f.IsEmbeddedDocument))
        {
            var embeddedFields = new List<FieldDefinition>
            {
                new() { Name = field.Name, ClrType = field.ClrType }
            };

            embeddedDocs.Add(new EmbeddedDocumentDefinition
            {
                Name = field.Name,
                Fields = embeddedFields,
                IsArray = field.IsArray
            });
        }

        return embeddedDocs;
    }

    public IReadOnlyList<ArrayRelationDefinition> DetectArrayRelations(
        DocumentDefinition document, IReadOnlyList<string> allCollections)
    {
        var relations = new List<ArrayRelationDefinition>();

        foreach (var field in document.Fields.Where(f => f.IsArray))
        {
            var referencedCollection = InferReferencedCollection(field.Name, allCollections);
            if (referencedCollection is not null)
            {
                relations.Add(new ArrayRelationDefinition
                {
                    FieldName = field.Name,
                    ReferencedCollection = referencedCollection,
                    ForeignField = "_id"
                });
            }
        }

        return relations;
    }

    private static string DetermineClrType(FieldStats stats)
    {
        if (stats.TypeCounts.Count == 0)
            return "object";

        var primaryType = stats.TypeCounts.OrderByDescending(kv => kv.Value).First().Key;

        return primaryType switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Double" => "double",
            "Decimal" => "decimal",
            "Boolean" => "bool",
            "String" => "string",
            "DateTime" => "DateTime",
            "Guid" => "Guid",
            "Byte[]" => "byte[]",
            "Dictionary" => "object",
            "List`1" => "object",
            _ => "string"
        };
    }

    private static bool IsIdField(string fieldName)
    {
        var lower = fieldName.ToLowerInvariant();
        return lower is "_id" or "id" or "objectid";
    }

    private static string? InferReferencedCollection(string fieldName, IReadOnlyList<string> collections)
    {
        var cleanName = fieldName
            .Replace("Ids", "")
            .Replace("ids", "")
            .Replace("ID", "")
            .Replace("Id", "")
            .Replace("List", "")
            .TrimEnd('s');

        return collections.FirstOrDefault(c =>
            c.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FieldStats
    {
        public Dictionary<string, int> TypeCounts { get; } = new();
        public int TotalCount { get; private set; }
        public string? PrimaryOriginalType { get; private set; }
        public double OccurrenceRate => TotalCount > 0 ? 1.0 : 0.0;

        public void Update(object value)
        {
            TotalCount++;
            var typeName = GetTypeName(value);
            TypeCounts[typeName] = TypeCounts.GetValueOrDefault(typeName) + 1;
            PrimaryOriginalType ??= typeName;
        }

        private static string GetTypeName(object value) => value switch
        {
            null => "Null",
            int => "Int32",
            long => "Int64",
            double => "Double",
            decimal => "Decimal",
            bool => "Boolean",
            string s when Guid.TryParse(s, out _) => "Guid",
            string => "String",
            DateTime => "DateTime",
            byte[] => "Byte[]",
            Dictionary<string, object> => "Dictionary",
            System.Collections.IList => "List`1",
            _ => value.GetType().Name
        };
    }
}
