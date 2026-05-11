using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.ValueObjects;

namespace Generador.CharmelCodeIA.Tests.Unit.Domain;

public sealed class ConnectionStringTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var cs = new ConnectionString(
            DatabaseProviderType.SqlServer, "localhost", 1433, "TestDb", "sa", "pass");

        Assert.Equal(DatabaseProviderType.SqlServer, cs.Provider);
        Assert.Equal("localhost", cs.Server);
        Assert.Equal(1433, cs.Port);
        Assert.Equal("TestDb", cs.Database);
    }

    [Fact]
    public void Constructor_WithZeroPort_UsesDefaultPort()
    {
        var cs = new ConnectionString(
            DatabaseProviderType.PostgreSql, "localhost", 0, "TestDb", "user", "pass");

        Assert.Equal(5432, cs.Port);
    }

    [Fact]
    public void Constructor_WithEmptyServer_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConnectionString(DatabaseProviderType.SqlServer, "", 1433, "db", "u", "p"));
    }

    [Fact]
    public void BuildConnectionString_SqlServer_ReturnsCorrectFormat()
    {
        var cs = new ConnectionString(
            DatabaseProviderType.SqlServer, "localhost", 1433, "TestDb", "sa", "pass");

        var result = cs.BuildConnectionString();

        Assert.Contains("Server=localhost,1433", result);
        Assert.Contains("Database=TestDb", result);
        Assert.Contains("TrustServerCertificate=True", result);
    }

    [Fact]
    public void BuildConnectionString_PostgreSql_ReturnsCorrectFormat()
    {
        var cs = new ConnectionString(
            DatabaseProviderType.PostgreSql, "pg.example.com", 5432, "mydb", "admin", "secret");

        var result = cs.BuildConnectionString();

        Assert.Contains("Host=pg.example.com", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=mydb", result);
    }

    [Fact]
    public void BuildConnectionString_MongoDB_ReturnsCorrectFormat()
    {
        var cs = new ConnectionString(
            DatabaseProviderType.MongoDB, "mongo.example.com", 27017, "mydb", "admin", "secret");

        var result = cs.BuildConnectionString();

        Assert.Contains("mongodb://admin:secret@mongo.example.com:27017/mydb", result);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var cs1 = new ConnectionString(DatabaseProviderType.SqlServer, "localhost", 1433, "test", "u", "p");
        var cs2 = new ConnectionString(DatabaseProviderType.SqlServer, "localhost", 1433, "test", "u", "p");

        Assert.Equal(cs1, cs2);
        Assert.True(cs1 == cs2);
    }

    [Fact]
    public void Equality_DifferentServer_AreNotEqual()
    {
        var cs1 = new ConnectionString(DatabaseProviderType.SqlServer, "localhost", 1433, "test", "u", "p");
        var cs2 = new ConnectionString(DatabaseProviderType.SqlServer, "other", 1433, "test", "u", "p");

        Assert.NotEqual(cs1, cs2);
    }
}
