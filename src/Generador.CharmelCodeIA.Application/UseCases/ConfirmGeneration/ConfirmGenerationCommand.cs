using Generador.CharmelCodeIA.Domain.Entities;
using MediatR;

namespace Generador.CharmelCodeIA.Application.UseCases.ConfirmGeneration;

public sealed record ConfirmGenerationCommand : IRequest<ConfirmGenerationResult>
{
    public DifferentialResult Differential { get; init; } = null!;
    public string OutputPath { get; init; } = string.Empty;
    public IReadOnlyList<string> ConfirmedFiles { get; init; } = Array.Empty<string>();
    public bool ApplyAll { get; init; }
}

public sealed record ConfirmGenerationResult
{
    public bool Success { get; init; }
    public int FilesWritten { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
