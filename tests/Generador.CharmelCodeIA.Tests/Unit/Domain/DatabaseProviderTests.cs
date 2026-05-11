using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.ValueObjects;

namespace Generador.CharmelCodeIA.Tests.Unit.Domain;

public sealed class DatabaseProviderTests
{
    [Fact]
    public void All_HasSixProviders()
    {
        var all = DatabaseProvider.All;
        Assert.Equal(6, all.Count);
    }

    [Fact]
    public void SqlServer_IsRelational()
    {
        Assert.True(DatabaseProvider.SqlServer.IsRelational);
        Assert.False(DatabaseProvider.SqlServer.IsDocument);
    }

    [Fact]
    public void MongoDB_IsDocument()
    {
        Assert.False(DatabaseProvider.MongoDB.IsRelational);
        Assert.True(DatabaseProvider.MongoDB.IsDocument);
    }

    [Fact]
    public void FromType_SqlServer_ReturnsCorrectInstance()
    {
        var provider = DatabaseProvider.FromType(DatabaseProviderType.SqlServer);
        Assert.Equal("SQL Server", provider.DisplayName);
        Assert.Equal("dbo", provider.DefaultSchema);
    }

    [Fact]
    public void FromType_InvalidType_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DatabaseProvider.FromType((DatabaseProviderType)99));
    }

    [Fact]
    public void Equality_SameType_ReturnsTrue()
    {
        var p1 = DatabaseProvider.SqlServer;
        var p2 = DatabaseProvider.SqlServer;
        Assert.Equal(p1, p2);
    }
}
