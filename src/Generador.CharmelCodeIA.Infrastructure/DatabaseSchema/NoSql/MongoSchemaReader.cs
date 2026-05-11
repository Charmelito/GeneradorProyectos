using MongoDB.Driver;
using MongoDB.Bson;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using DatabaseSchemaEntity = Generador.CharmelCodeIA.Domain.Entities.DatabaseSchema;

namespace Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.NoSql;

public sealed class MongoSchemaReader : IDatabaseSchemaReader
{
    private readonly ISchemaInferenceStrategy _inferenceEngine;
    private const int DefaultSampleSize = 100;

    public MongoSchemaReader(ISchemaInferenceStrategy inferenceEngine)
    {
        _inferenceEngine = inferenceEngine;
    }

    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var client = new MongoClient(connectionString);
        var databaseName = MongoUrl.Create(connectionString).DatabaseName;
        if (string.IsNullOrEmpty(databaseName)) return false;
        var database = client.GetDatabase(databaseName);
        var collections = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        await collections.FirstOrDefaultAsync(cancellationToken);
        return true;
    }

    public async Task<DatabaseSchemaEntity> ReadSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var client = new MongoClient(connectionString);
        var url = MongoUrl.Create(connectionString);
        var databaseName = url.DatabaseName;

        if (string.IsNullOrEmpty(databaseName))
            throw new ArgumentException("MongoDB connection string must include database name.");

        var database = client.GetDatabase(databaseName);
        var collectionNames = await database.ListCollectionNames().ToListAsync(cancellationToken);

        var definitions = new List<DocumentDefinition>();
        var allCollectionNames = collectionNames.ToList();

        foreach (var collectionName in allCollectionNames)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var sampleDocs = await collection.Find(FilterDefinition<BsonDocument>.Empty)
                .Limit(DefaultSampleSize).ToListAsync(cancellationToken);

            var dictionaries = sampleDocs.Select(BsonDocumentToDictionary).ToList();
            var definition = _inferenceEngine.InferSchema(collectionName, dictionaries);
            definitions.Add(definition);
        }

        var relationships = _inferenceEngine.DetectArrayRelations(
            definitions.FirstOrDefault() ?? new DocumentDefinition(), allCollectionNames)
            .Select(arr => new RelationshipDefinition
            {
                Name = $"FK_{arr.ReferencedCollection}",
                DependentTable = arr.ReferencedCollection,
                DependentColumn = arr.FieldName,
                PrincipalTable = arr.ReferencedCollection,
                PrincipalColumn = arr.ForeignField,
                Type = RelationshipType.OneToMany,
                IsRequired = false
            }).ToList();

        var tables = definitions.Select(d => new TableDefinition
        {
            Name = d.CollectionName,
            Schema = databaseName,
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
            DatabaseName = databaseName,
            Provider = DatabaseProviderType.MongoDB,
            Tables = tables,
            Relationships = relationships,
            Collections = definitions,
            SampleSize = DefaultSampleSize,
            ReadAt = DateTime.UtcNow
        };
    }

    private static Dictionary<string, object> BsonDocumentToDictionary(BsonDocument doc)
    {
        var dict = new Dictionary<string, object>();
        foreach (var element in doc.Elements)
            dict[element.Name] = BsonValueToObject(element.Value);
        return dict;
    }

    private static object BsonValueToObject(BsonValue value) => value.BsonType switch
    {
        BsonType.Null => null!,
        BsonType.Boolean => value.AsBoolean,
        BsonType.Int32 => value.AsInt32,
        BsonType.Int64 => value.AsInt64,
        BsonType.Double => value.AsDouble,
        BsonType.Decimal128 => (decimal)value.AsDecimal128,
        BsonType.String => value.AsString,
        BsonType.DateTime => value.ToUniversalTime(),
        BsonType.ObjectId => value.AsObjectId.ToString(),
        BsonType.Binary => value.AsByteArray,
        BsonType.Document => BsonDocumentToDictionary(value.AsBsonDocument),
        BsonType.Array => value.AsBsonArray.Select(BsonValueToObject).ToList(),
        _ => value.ToString()!
    };
}
