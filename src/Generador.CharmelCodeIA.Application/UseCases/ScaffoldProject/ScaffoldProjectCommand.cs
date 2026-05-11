using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using MediatR;

namespace Generador.CharmelCodeIA.Application.UseCases.ScaffoldProject;

public sealed record ScaffoldProjectCommand : IRequest<ScaffoldProjectResult>
{
    public string CompanyName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public GenerationMode Mode { get; init; } = GenerationMode.FullSolution;
    public IReadOnlyList<ProjectLayer> Layers { get; init; } = Array.Empty<ProjectLayer>();
    public IReadOnlyDictionary<string, string> GeneratedFiles { get; init; } = new Dictionary<string, string>();
    public DifferentialResult? Differential { get; init; }
}

public sealed record ScaffoldProjectResult
{
    public bool Success { get; init; }
    public string SolutionPath { get; init; } = string.Empty;
    public IReadOnlyList<string> CreatedFiles { get; init; } = Array.Empty<string>();
    public string? ErrorMessage { get; init; }
}
