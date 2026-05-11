using Generador.CharmelCodeIA.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.Services;

public sealed class DifferentialService
{
    private readonly ILogger<DifferentialService> _logger;

    public DifferentialService(ILogger<DifferentialService> logger)
    {
        _logger = logger;
    }

    public DifferentialResult Analyze(
        string projectPath,
        IReadOnlyDictionary<string, string> proposedFiles)
    {
        var changes = new List<FileChange>();

        foreach (var (relativePath, proposedContent) in proposedFiles)
        {
            var fullPath = Path.Combine(projectPath, relativePath);

            if (!File.Exists(fullPath))
            {
                changes.Add(new FileChange
                {
                    RelativePath = relativePath,
                    Type = ChangeType.New,
                    ProposedContent = proposedContent,
                    IsConfirmed = false
                });
                continue;
            }

            var existingContent = File.ReadAllText(fullPath);

            if (existingContent == proposedContent)
            {
                changes.Add(new FileChange
                {
                    RelativePath = relativePath,
                    Type = ChangeType.Unchanged,
                    ExistingContent = existingContent,
                    ProposedContent = proposedContent,
                    IsConfirmed = true
                });
                continue;
            }

            var conflicts = DetectConflicts(existingContent, proposedContent);
            changes.Add(new FileChange
            {
                RelativePath = relativePath,
                Type = conflicts.Any() ? ChangeType.Conflict : ChangeType.Modified,
                ExistingContent = existingContent,
                ProposedContent = proposedContent,
                Diff = GenerateDiff(existingContent, proposedContent),
                IsConfirmed = false,
                Conflicts = conflicts
            });
        }

        var summary = new DifferentialSummary
        {
            NewFiles = changes.Count(c => c.Type == ChangeType.New),
            ModifiedFiles = changes.Count(c => c.Type == ChangeType.Modified),
            UnchangedFiles = changes.Count(c => c.Type == ChangeType.Unchanged),
            ConflictFiles = changes.Count(c => c.Type == ChangeType.Conflict)
        };

        _logger.LogInformation("Diff analysis: {New}N {Mod}M {Unch}U {Conf}C",
            summary.NewFiles, summary.ModifiedFiles,
            summary.UnchangedFiles, summary.ConflictFiles);

        return new DifferentialResult
        {
            ProjectPath = projectPath,
            Changes = changes,
            Summary = summary,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private static List<FileConflict> DetectConflicts(string existing, string proposed)
    {
        var conflicts = new List<FileConflict>();
        var existingLines = existing.Split('\n');
        var proposedLines = proposed.Split('\n');

        // Simple conflict detection: if overall line count differs significantly
        if (Math.Abs(existingLines.Length - proposedLines.Length) > existingLines.Length * 0.5)
        {
            conflicts.Add(new FileConflict
            {
                StartLine = 1,
                EndLine = existingLines.Length,
                Description = "File structure differs significantly from proposed version",
                Severity = ConflictSeverity.High
            });
        }

        return conflicts;
    }

    private static string GenerateDiff(string existing, string proposed)
    {
        var existingLines = existing.Split('\n');
        var proposedLines = proposed.Split('\n');
        var diffLines = new List<string>();

        var maxLen = Math.Max(existingLines.Length, proposedLines.Length);
        for (var i = 0; i < maxLen; i++)
        {
            var existingLine = i < existingLines.Length ? existingLines[i].TrimEnd('\r') : null;
            var proposedLine = i < proposedLines.Length ? proposedLines[i].TrimEnd('\r') : null;

            if (existingLine == proposedLine)
            {
                if (existingLine != null)
                    diffLines.Add($"  {existingLine}");
            }
            else
            {
                if (existingLine != null)
                    diffLines.Add($"- {existingLine}");
                if (proposedLine != null)
                    diffLines.Add($"+ {proposedLine}");
            }
        }

        return string.Join('\n', diffLines);
    }
}
