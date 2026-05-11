using Generador.CharmelCodeIA.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Infrastructure.ProjectScaffolding;

public sealed class IncrementalScaffolder
{
    private readonly ILogger<IncrementalScaffolder> _logger;

    public IncrementalScaffolder(ILogger<IncrementalScaffolder> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ApplyChangesAsync(
        string projectPath,
        DifferentialResult differential,
        CancellationToken ct = default)
    {
        var writtenFiles = new List<string>();
        var changes = differential.Changes
            .Where(c => c.IsConfirmed && c.Type != ChangeType.Unchanged)
            .ToList();

        foreach (var change in changes)
        {
            var fullPath = Path.Combine(projectPath, change.RelativePath);
            var dir = Path.GetDirectoryName(fullPath)!;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (change.Type == ChangeType.Conflict)
            {
                _logger.LogWarning("Conflict file skipped (needs manual resolution): {Path}", change.RelativePath);
                continue;
            }

            await File.WriteAllTextAsync(fullPath, change.ProposedContent, ct);
            writtenFiles.Add(change.RelativePath);

            _logger.LogInformation(
                change.Type == ChangeType.New ? "Created: {Path}" : "Modified: {Path}",
                change.RelativePath);
        }

        return writtenFiles;
    }

    public async Task AddFilesToProjectAsync(
        string projectPath,
        IReadOnlyDictionary<string, string> files,
        CancellationToken ct = default)
    {
        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(projectPath, relativePath);
            var dir = Path.GetDirectoryName(fullPath)!;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(fullPath))
            {
                await File.WriteAllTextAsync(fullPath, content, ct);
                _logger.LogInformation("Added: {Path}", relativePath);
            }
        }
    }
}
