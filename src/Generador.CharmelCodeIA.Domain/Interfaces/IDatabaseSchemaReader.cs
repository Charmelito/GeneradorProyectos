using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface IDatabaseSchemaReader
{
    Task<DatabaseSchema> ReadSchemaAsync(string connectionString, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default);
}
