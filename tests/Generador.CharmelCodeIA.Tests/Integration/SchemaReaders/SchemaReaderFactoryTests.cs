using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Infrastructure;
using Generador.CharmelCodeIA.Infrastructure.DatabaseSchema;
using Generador.CharmelCodeIA.Infrastructure.DatabaseSchema.NoSql;
using Microsoft.Extensions.DependencyInjection;

namespace Generador.CharmelCodeIA.Tests.Integration.SchemaReaders;

public sealed class SchemaReaderFactoryTests
{
    [Fact]
    public void Create_SqlServer_ReturnsNonNullReader()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        var sp = services.BuildServiceProvider();

        var factory = new SchemaReaderFactory(sp);
        var reader = factory.Create(DatabaseProviderType.SqlServer);

        Assert.NotNull(reader);
    }

    [Fact]
    public void Create_MongoDB_ReturnsNonNullReader()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        var sp = services.BuildServiceProvider();

        var factory = new SchemaReaderFactory(sp);
        var reader = factory.Create(DatabaseProviderType.MongoDB);

        Assert.NotNull(reader);
    }

    [Fact]
    public void Create_AllProviders_ReturnNonNullReaders()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        var sp = services.BuildServiceProvider();

        var factory = new SchemaReaderFactory(sp);

        foreach (var providerType in Enum.GetValues<DatabaseProviderType>())
        {
            var reader = factory.Create(providerType);
            Assert.NotNull(reader);
        }
    }
}
