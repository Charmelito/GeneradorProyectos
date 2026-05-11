using Generador.CharmelCodeIA.Application.Services;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;

namespace Generador.CharmelCodeIA.Tests.Unit.Domain;

public sealed class SchemaAnalyzerTests
{
    private readonly SchemaAnalyzer _analyzer = new();

    [Fact]
    public void DetectValueObjects_EmailColumn_ReturnsEmailCandidate()
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = "TestDb",
            Provider = DatabaseProviderType.SqlServer,
            Tables = new List<TableDefinition>
            {
                new()
                {
                    Name = "Users",
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", ClrType = "int", IsPrimaryKey = true },
                        new() { Name = "Email", ClrType = "string", MaxLength = 256 }
                    }
                }
            }
        };

        var candidates = _analyzer.DetectValueObjects(schema);

        Assert.Contains(candidates, c => c.Name == "Email" && c.TableName == "Users");
    }

    [Fact]
    public void DetectValueObjects_MoneyColumns_ReturnsMoneyCandidate()
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = "TestDb",
            Provider = DatabaseProviderType.SqlServer,
            Tables = new List<TableDefinition>
            {
                new()
                {
                    Name = "Orders",
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", ClrType = "int", IsPrimaryKey = true },
                        new() { Name = "TotalAmount", ClrType = "decimal" },
                        new() { Name = "Currency", ClrType = "string" }
                    }
                }
            }
        };

        var candidates = _analyzer.DetectValueObjects(schema);

        Assert.Contains(candidates, c => c.Name == "Money" && c.TableName == "Orders");
    }

    [Fact]
    public void DetectValueObjects_AddressColumns_ReturnsAddressCandidate()
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = "TestDb",
            Provider = DatabaseProviderType.SqlServer,
            Tables = new List<TableDefinition>
            {
                new()
                {
                    Name = "Customers",
                    Columns = new List<ColumnDefinition>
                    {
                        new() { Name = "Id", ClrType = "int", IsPrimaryKey = true },
                        new() { Name = "Street", ClrType = "string" },
                        new() { Name = "City", ClrType = "string" },
                        new() { Name = "State", ClrType = "string" }
                    }
                }
            }
        };

        var candidates = _analyzer.DetectValueObjects(schema);

        Assert.Contains(candidates, c => c.Name == "Address" && c.TableName == "Customers");
    }

    [Fact]
    public void DetectValueObjects_EmptySchema_ReturnsEmptyList()
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = "TestDb",
            Provider = DatabaseProviderType.SqlServer,
            Tables = Array.Empty<TableDefinition>()
        };

        var candidates = _analyzer.DetectValueObjects(schema);

        Assert.Empty(candidates);
    }
}
