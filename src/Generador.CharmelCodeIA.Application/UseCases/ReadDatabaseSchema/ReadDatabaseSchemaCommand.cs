using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using MediatR;

namespace Generador.CharmelCodeIA.Application.UseCases.ReadDatabaseSchema;

public sealed record ReadDatabaseSchemaCommand : IRequest<ReadDatabaseSchemaResult>
{
    public string ConnectionString { get; init; } = string.Empty;
    public DatabaseProviderType Provider { get; init; }
    public bool TestOnly { get; init; }
}

public sealed record ReadDatabaseSchemaResult
{
    public bool Success { get; init; }
    public bool ConnectionValid { get; init; }
    public DatabaseSchema? Schema { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Summary { get; init; }
}
