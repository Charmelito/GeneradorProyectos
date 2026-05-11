using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.ValueObjects;
using Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.NoSql;

namespace Generador.CharmelCodeIA.Tests.Integration.SchemaReaders;

public sealed class SchemaInferenceEngineTests
{
    private readonly SchemaInferenceEngine _engine = new();

    [Fact]
    public void InferSchema_WithSampleDocuments_ReturnsValidDefinition()
    {
        var sampleDocs = new List<Dictionary<string, object>>
        {
            new()
            {
                ["_id"] = "abc123",
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
                ["age"] = 30,
                ["isActive"] = true,
                ["createdAt"] = DateTime.UtcNow
            }
        };

        var definition = _engine.InferSchema("users", sampleDocs);

        Assert.Equal("users", definition.CollectionName);
        Assert.NotEmpty(definition.Fields);
        Assert.Contains(definition.Fields, f => f.Name == "email");
        Assert.Contains(definition.Fields, f => f.IsId);
    }

    [Fact]
    public void InferFieldType_StringValues_ReturnsString()
    {
        var sampleValues = new List<object> { "hello", "world", "test" };
        var field = _engine.InferFieldType("email", sampleValues);

        Assert.Equal("string", field.ClrType);
    }

    [Fact]
    public void InferFieldType_IntValues_ReturnsInt()
    {
        var sampleValues = new List<object> { 1, 2, 3, 4 };
        var field = _engine.InferFieldType("count", sampleValues);

        Assert.Equal("int", field.ClrType);
    }

    [Fact]
    public void InferSchema_EmptyDocuments_ReturnsEmptyFields()
    {
        var sampleDocs = new List<Dictionary<string, object>>();

        var definition = _engine.InferSchema("empty", sampleDocs);

        Assert.Empty(definition.Fields);
    }

    [Fact]
    public void InferSchema_IdField_IsDetectedAsPrimaryKey()
    {
        var sampleDocs = new List<Dictionary<string, object>>
        {
            new() { ["_id"] = "abc", ["name"] = "Test" }
        };

        var definition = _engine.InferSchema("test", sampleDocs);

        var idField = definition.Fields.FirstOrDefault(f => f.IsId);
        Assert.NotNull(idField);
        Assert.Equal("_id", idField.Name);
    }
}
