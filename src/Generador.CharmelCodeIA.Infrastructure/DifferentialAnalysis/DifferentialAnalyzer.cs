using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Infrastructure.DifferentialAnalysis;

public sealed class DifferentialAnalyzer : IDifferentialAnalyzer
{
    private readonly ProjectFileScanner _fileScanner;
    private readonly ConflictResolver _conflictResolver;
    private readonly ILogger<DifferentialAnalyzer> _logger;

    public DifferentialAnalyzer(
        ProjectFileScanner fileScanner,
        ConflictResolver conflictResolver,
        ILogger<DifferentialAnalyzer> logger)
    {
        _fileScanner = fileScanner;
        _conflictResolver = conflictResolver;
        _logger = logger;
    }

    public async Task<DifferentialResult> AnalyzeAsync(
        string projectPath,
        IReadOnlyDictionary<string, string> proposedFiles,
        CancellationToken cancellationToken = default)
    {
        var changes = new List<FileChange>();
        var existingFiles = await _fileScanner.ScanAsync(projectPath, cancellationToken);

        foreach (var (relativePath, proposedContent) in proposedFiles)
        {
            if (existingFiles.TryGetValue(relativePath, out var existingContent))
            {
                changes.Add(CompareFile(relativePath, existingContent, proposedContent));
            }
            else
            {
                changes.Add(new FileChange
                {
                    RelativePath = relativePath,
                    Type = ChangeType.New,
                    ProposedContent = proposedContent,
                    IsConfirmed = false
                });
            }
        }

        var summary = new DifferentialSummary
        {
            NewFiles = changes.Count(c => c.Type == ChangeType.New),
            ModifiedFiles = changes.Count(c => c.Type == ChangeType.Modified),
            UnchangedFiles = changes.Count(c => c.Type == ChangeType.Unchanged),
            ConflictFiles = changes.Count(c => c.Type == ChangeType.Conflict)
        };

        _logger.LogInformation(
            "Differential: {N}N {M}M {U}U {C}C",
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

    public FileChange CompareFile(string relativePath, string existingContent, string proposedContent)
    {
        if (existingContent == proposedContent)
        {
            return new FileChange
            {
                RelativePath = relativePath,
                Type = ChangeType.Unchanged,
                ExistingContent = existingContent,
                ProposedContent = proposedContent,
                IsConfirmed = true
            };
        }

        var conflicts = _conflictResolver.DetectConflicts(existingContent, proposedContent);

        return new FileChange
        {
            RelativePath = relativePath,
            Type = conflicts.Count > 0 ? ChangeType.Conflict : ChangeType.Modified,
            ExistingContent = existingContent,
            ProposedContent = proposedContent,
            Diff = GenerateUnifiedDiff(existingContent, proposedContent),
            IsConfirmed = false,
            Conflicts = conflicts
        };
    }

    public DifferentialResult MergeResults(DifferentialResult existing, DifferentialResult incoming)
    {
        var mergedChanges = new List<FileChange>();
        var existingByPath = existing.Changes.ToDictionary(c => c.RelativePath);

        foreach (var change in incoming.Changes)
        {
            if (existingByPath.TryGetValue(change.RelativePath, out var existingChange))
            {
                mergedChanges.Add(existingChange.IsConfirmed ? existingChange : change);
            }
            else
            {
                mergedChanges.Add(change);
            }
        }

        var summary = new DifferentialSummary
        {
            NewFiles = mergedChanges.Count(c => c.Type == ChangeType.New),
            ModifiedFiles = mergedChanges.Count(c => c.Type == ChangeType.Modified),
            UnchangedFiles = mergedChanges.Count(c => c.Type == ChangeType.Unchanged),
            ConflictFiles = mergedChanges.Count(c => c.Type == ChangeType.Conflict)
        };

        return new DifferentialResult
        {
            ProjectPath = existing.ProjectPath,
            Changes = mergedChanges,
            Summary = summary,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private static string GenerateUnifiedDiff(string existing, string proposed)
    {
        var existingLines = existing.Split('\n');
        var proposedLines = proposed.Split('\n');
        var diffLines = new List<string>();
        var maxLen = Math.Max(existingLines.Length, proposedLines.Length);

        for (var i = 0; i < maxLen; i++)
        {
            var eLine = i < existingLines.Length ? existingLines[i].TrimEnd('\r') : null;
            var pLine = i < proposedLines.Length ? proposedLines[i].TrimEnd('\r') : null;

            if (eLine == pLine)
            {
                if (eLine != null) diffLines.Add($"  {eLine}");
            }
            else
            {
                if (eLine != null) diffLines.Add($"- {eLine}");
                if (pLine != null) diffLines.Add($"+ {pLine}");
            }
        }

        return string.Join('\n', diffLines);
    }
}
