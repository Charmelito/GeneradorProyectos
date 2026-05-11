using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface IUseCaseGenerator
{
    Task<string> GenerateCommandAsync(UseCaseDefinition useCase, DatabaseSchema schema, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateQueryAsync(UseCaseDefinition useCase, DatabaseSchema schema, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateHandlerAsync(UseCaseDefinition useCase, DatabaseSchema schema, ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task<string> GenerateResultAsync(UseCaseDefinition useCase, DatabaseSchema schema, ProjectConfiguration config, CancellationToken cancellationToken = default);
}
