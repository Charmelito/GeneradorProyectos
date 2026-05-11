using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface ISchemaInferenceStrategy
{
    DocumentDefinition InferSchema(string collectionName, IReadOnlyList<Dictionary<string, object>> sampleDocuments);
    FieldDefinition InferFieldType(string fieldName, IReadOnlyList<object> sampleValues);
    IReadOnlyList<EmbeddedDocumentDefinition> DetectEmbeddedDocuments(DocumentDefinition document);
    IReadOnlyList<ArrayRelationDefinition> DetectArrayRelations(DocumentDefinition document, IReadOnlyList<string> allCollections);
}
