using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;

namespace Generador.CharmelCodeIA.Tests.Unit.Domain;

public sealed class EntityModelTests
{
    [Fact]
    public void DatabaseSchema_DefaultValues_AreCorrect()
    {
        var schema = new DatabaseSchema();

        Assert.Equal(string.Empty, schema.DatabaseName);
        Assert.Empty(schema.Tables);
        Assert.Empty(schema.Relationships);
    }

    [Fact]
    public void ProjectConfiguration_DefaultMode_IsFullSolution()
    {
        var config = new ProjectConfiguration();

        Assert.Equal(GenerationMode.FullSolution, config.Mode);
    }

    [Fact]
    public void UseCaseDefinition_DefaultType_IsZero()
    {
        var uc = new UseCaseDefinition();

        Assert.Equal(string.Empty, uc.EntityName);
        Assert.Equal((UseCaseType)0, uc.Type);
    }

    [Fact]
    public void DifferentialSummary_TotalChanges_CalculatesCorrectly()
    {
        var summary = new DifferentialSummary
        {
            NewFiles = 5,
            ModifiedFiles = 3,
            ConflictFiles = 2,
            UnchangedFiles = 40
        };

        Assert.Equal(10, summary.TotalChanges);
    }

    [Fact]
    public void ColumnDefinition_Defaults_AreCorrect()
    {
        var col = new ColumnDefinition();

        Assert.Equal(string.Empty, col.Name);
        Assert.False(col.IsPrimaryKey);
        Assert.False(col.IsForeignKey);
        Assert.Equal(0, col.OrdinalPosition);
    }

    [Fact]
    public void RelationshipDefinition_Type_IsOneToMany()
    {
        var rel = new RelationshipDefinition { Type = RelationshipType.OneToOne };

        Assert.Equal(RelationshipType.OneToOne, rel.Type);
    }
}
