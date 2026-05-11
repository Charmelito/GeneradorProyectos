using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.ValueObjects;
using MediatR;

namespace Generador.CharmelCodeIA.Application.UseCases.GenerateEntities;

public sealed record GenerateEntitiesCommand : IRequest<GenerateEntitiesResult>
{
    public DatabaseSchema Schema { get; init; } = null!;
    public string CompanyName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public bool GenerateConfigurations { get; init; } = true;
    public bool GenerateDbContext { get; init; } = true;
}

public sealed record GenerateEntitiesResult
{
    public bool Success { get; init; }
    public IReadOnlyDictionary<string, string> GeneratedFiles { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
