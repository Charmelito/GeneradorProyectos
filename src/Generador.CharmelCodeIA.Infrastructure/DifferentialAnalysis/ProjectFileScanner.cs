using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Infrastructure.DifferentialAnalysis;

public sealed class ProjectFileScanner
{
    private readonly ILogger<ProjectFileScanner> _logger;

    public ProjectFileScanner(ILogger<ProjectFileScanner> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyDictionary<string, string>> ScanAsync(
        string projectPath, CancellationToken ct = default)
    {
        var files = new Dictionary<string, string>();

        if (!Directory.Exists(projectPath))
        {
            _logger.LogWarning("Project path does not exist: {Path}", projectPath);
            return Task.FromResult<IReadOnlyDictionary<string, string>>(files);
        }

        var allFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f))
            .ToList();

        foreach (var filePath in allFiles)
        {
            var relativePath = Path.GetRelativePath(projectPath, filePath);
            try
            {
                files[relativePath] = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read file: {Path}", relativePath);
            }
        }

        _logger.LogInformation("Scanned {Count} files in {Path}", files.Count, projectPath);
        return Task.FromResult<IReadOnlyDictionary<string, string>>(files);
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(
        string projectPath, string searchPattern = "*.*", CancellationToken ct = default)
    {
        if (!Directory.Exists(projectPath))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var files = Directory.GetFiles(projectPath, searchPattern, SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f))
            .Select(f => Path.GetRelativePath(projectPath, f))
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public bool FileExists(string projectPath, string relativePath)
    {
        var fullPath = Path.Combine(projectPath, relativePath);
        return File.Exists(fullPath);
    }

    public string? ReadFile(string projectPath, string relativePath)
    {
        var fullPath = Path.Combine(projectPath, relativePath);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
    }

    private static bool IsExcluded(string filePath)
    {
        var relativePath = filePath.Replace('\\', '/');
        return relativePath.Contains("/obj/") ||
               relativePath.Contains("/bin/") ||
               relativePath.Contains("/.git/") ||
               relativePath.Contains("/.vs/") ||
               relativePath.Contains("/node_modules/") ||
               relativePath.EndsWith(".user") ||
               relativePath.EndsWith(".suo") ||
               relativePath.EndsWith(".cache");
    }
}
