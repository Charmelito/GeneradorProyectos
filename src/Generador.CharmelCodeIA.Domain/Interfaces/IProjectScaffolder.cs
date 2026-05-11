using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface IProjectScaffolder
{
    Task ScaffoldFullSolutionAsync(ProjectConfiguration config, CancellationToken cancellationToken = default);
    Task ScaffoldIncrementalAsync(ProjectConfiguration config, DifferentialResult differential, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetGeneratedFilesAsync(string projectPath);
}
