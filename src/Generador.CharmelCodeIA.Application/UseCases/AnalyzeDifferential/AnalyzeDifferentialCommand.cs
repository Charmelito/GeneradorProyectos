using Generador.CharmelCodeIA.Domain.Entities;
using MediatR;

namespace Generador.CharmelCodeIA.Application.UseCases.AnalyzeDifferential;

public sealed record AnalyzeDifferentialCommand : IRequest<AnalyzeDifferentialResult>
{
    public string ProjectPath { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> ProposedFiles { get; init; } = new Dictionary<string, string>();
}

public sealed record AnalyzeDifferentialResult
{
    public bool Success { get; init; }
    public DifferentialResult? Differential { get; init; }
    public string? ErrorMessage { get; init; }
}
