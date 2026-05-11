using Microsoft.Azure.Cosmos;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using System.Text.Json;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.NoSql;

public sealed class CosmosSchemaReader : IDatabaseSchemaReader
{
    private readonly ISchemaInferenceStrategy _inferenceEngine;
    private const int DefaultSampleSize = 100;

    public CosmosSchemaReader(ISchemaInferenceStrategy inferenceEngine)
    {
        _inferenceEngine = inferenceEngine;
    }

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var client = new CosmosClient(connectionString);
        var accountProperties = await client.ReadAccountAsync();
        return accountProperties != null;
    }

    public async Task<DatabaseSchemaEntity> ReadSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var client = new CosmosClient(connectionString);
        var definitions = new List<DocumentDefinition>();
        var allCollectionNames = new List<string>();

        var databaseIterator = client.GetDatabaseQueryIterator<DatabaseProperties>();
        while (databaseIterator.HasMoreResults)
        {
            var databases = await databaseIterator.ReadNextAsync(cancellationToken);
            foreach (var databaseProps in databases)
            {
                var database = client.GetDatabase(databaseProps.Id);
                var containerIterator = database.GetContainerQueryIterator<ContainerProperties>();

                while (containerIterator.HasMoreResults)
                {
                    var containers = await containerIterator.ReadNextAsync(cancellationToken);
                    foreach (var containerProps in containers)
                    {
                        allCollectionNames.Add(containerProps.Id);
                        var container = database.GetContainer(containerProps.Id);
                        var sampleDocs = await SampleDocumentsAsync(container, DefaultSampleSize, cancellationToken);
                        var definition = _inferenceEngine.InferSchema(containerProps.Id, sampleDocs);
                        definition.PartitionKey = containerProps.PartitionKeyPath ?? "/id";
                        definitions.Add(definition);
                    }
                }
            }
        }

        var relationships = new List<RelationshipDefinition>();
        foreach (var doc in definitions)
        {
            var arrayRelations = _inferenceEngine.DetectArrayRelations(doc, allCollectionNames);
            foreach (var arr in arrayRelations)
            {
                relationships.Add(new RelationshipDefinition
                {
                    Name = $"FK_{doc.CollectionName}_{arr.ReferencedCollection}",
                    DependentTable = doc.CollectionName,
                    DependentColumn = arr.FieldName,
                    PrincipalTable = arr.ReferencedCollection,
                    PrincipalColumn = arr.ForeignField,
                    Type = RelationshipType.OneToMany,
                    IsRequired = false
                });
            }
        }

        var tables = definitions.Select(d => new TableDefinition
        {
            Name = d.CollectionName,
            Schema = d.PartitionKey,
            Columns = d.Fields.Select(f => new ColumnDefinition
            {
                Name = f.Name,
                ClrType = f.ClrType,
                SqlType = f.OriginalType,
                IsNullable = !f.IsRequired,
                IsPrimaryKey = f.IsId,
                OrdinalPosition = 0
            }).ToList()
        }).ToList();

        return new DocumentSchema
        {
            DatabaseName = "CosmosDB",
            Provider = DatabaseProviderType.CosmosDB,
            Tables = tables,
            Relationships = relationships,
            Collections = definitions,
            SampleSize = DefaultSampleSize,
            ReadAt = DateTime.UtcNow
        };
    }

    private static async Task<List<Dictionary<string, object>>> SampleDocumentsAsync(
        Container container, int sampleSize, CancellationToken ct)
    {
        var documents = new List<Dictionary<string, object>>();
        var query = new QueryDefinition("SELECT * FROM c OFFSET 0 LIMIT @limit")
            .WithParameter("@limit", sampleSize);

        var iterator = container.GetItemQueryIterator<JsonElement>(query);
        while (iterator.HasMoreResults && documents.Count < sampleSize)
        {
            var response = await iterator.ReadNextAsync(ct);
            foreach (var element in response)
                documents.Add(JsonElementToDictionary(element));
        }

        return documents;
    }

    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        foreach (var property in element.EnumerateObject())
            dict[property.Name] = JsonElementToObject(property.Value);
        return dict;
    }

    private static object JsonElementToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null!,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal,
        JsonValueKind.Number when element.TryGetInt64(out var longVal) => longVal,
        JsonValueKind.Number when element.TryGetDouble(out var doubleVal) => doubleVal,
        JsonValueKind.Number => element.GetDecimal(),
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Object => JsonElementToDictionary(element),
        JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
        _ => element.ToString()
    };
}
