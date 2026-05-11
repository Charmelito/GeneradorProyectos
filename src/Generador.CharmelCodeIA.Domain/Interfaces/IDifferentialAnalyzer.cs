using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface IDifferentialAnalyzer
{
    Task<DifferentialResult> AnalyzeAsync(
        string projectPath,
        IReadOnlyDictionary<string, string> proposedFiles,
        CancellationToken cancellationToken = default);

    FileChange CompareFile(string relativePath, string existingContent, string proposedContent);
    DifferentialResult MergeResults(DifferentialResult existing, DifferentialResult incoming);
}
