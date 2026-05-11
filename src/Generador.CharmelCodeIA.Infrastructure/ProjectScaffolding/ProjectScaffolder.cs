using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Generador.CharmelCodeIA.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Infrastructure.ProjectScaffolding;

public sealed class ProjectScaffolder : IProjectScaffolder
{
    private readonly SolutionScaffolder _solutionScaffolder;
    private readonly LayerScaffolder _layerScaffolder;
    private readonly IncrementalScaffolder _incrementalScaffolder;
    private readonly ILogger<ProjectScaffolder> _logger;

    public ProjectScaffolder(
        SolutionScaffolder solutionScaffolder,
        LayerScaffolder layerScaffolder,
        IncrementalScaffolder incrementalScaffolder,
        ILogger<ProjectScaffolder> logger)
    {
        _solutionScaffolder = solutionScaffolder;
        _layerScaffolder = layerScaffolder;
        _incrementalScaffolder = incrementalScaffolder;
        _logger = logger;
    }

    public async Task ScaffoldFullSolutionAsync(
        ProjectConfiguration config, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scaffolding full solution: {Solution} → {Path}",
            config.Naming.SolutionName, config.OutputPath);

        var solutionDir = Path.Combine(config.OutputPath, config.Naming.SolutionName);

        await _solutionScaffolder.ScaffoldAsync(config, cancellationToken);

        foreach (var layer in config.LayersToGenerate.Distinct())
        {
            _layerScaffolder.CreateLayerStructure(config.OutputPath, config.Naming, layer);
        }

        if (config.GenerateUnitTestsProject)
        {
            _layerScaffolder.CreateLayerStructure(
                Path.Combine(config.OutputPath, config.Naming.SolutionName, "tests"),
                new NamingConvention(config.Naming.CompanyName, $"{config.Naming.ProjectName}.Tests"),
                ProjectLayer.Domain);
        }

        _logger.LogInformation("Solution scaffolded successfully");
    }

    public async Task ScaffoldIncrementalAsync(
        ProjectConfiguration config,
        DifferentialResult differential,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying incremental changes to {Path}", config.ExistingProjectPath);

        await _incrementalScaffolder.ApplyChangesAsync(
            config.ExistingProjectPath, differential, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetGeneratedFilesAsync(string projectPath)
    {
        if (!Directory.Exists(projectPath))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") && !f.Contains("\\.git\\"))
            .Select(f => Path.GetRelativePath(projectPath, f))
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }
}
