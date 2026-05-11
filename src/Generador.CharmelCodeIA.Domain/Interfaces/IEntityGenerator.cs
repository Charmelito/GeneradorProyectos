using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface IEntityGenerator
{
    Task<string> GenerateEntityAsync(TableDefinition table, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateConfigurationAsync(TableDefinition table, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateDocumentEntityAsync(DocumentDefinition collection, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateValueObjectAsync(IReadOnlyList<ColumnDefinition> columns, string valueObjectName, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateDbContextAsync(IReadOnlyList<TableDefinition> tables, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateRepositoryInterfaceAsync(string entityName, ProjectConfiguration config, CancellationToken cancellationToken = default);
}
