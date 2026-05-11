using Generador.CharmelCodeIA.Domain.Entities;
using MediatR;

namespace Generador.CharmelCodeIA.Application.UseCases.GenerateUseCases;

public sealed record GenerateUseCasesCommand : IRequest<GenerateUseCasesResult>
{
    public IReadOnlyList<UseCaseDefinition> UseCases { get; init; } = Array.Empty<UseCaseDefinition>();
    public DatabaseSchema Schema { get; init; } = null!;
    public string CompanyName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
}

public sealed record GenerateUseCasesResult
{
    public bool Success { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> GeneratedFiles { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, string>>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
