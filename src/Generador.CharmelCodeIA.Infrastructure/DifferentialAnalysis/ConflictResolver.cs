using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Infrastructure.DifferentialAnalysis;

public sealed class ConflictResolver
{
    public List<FileConflict> DetectConflicts(string existingContent, string proposedContent)
    {
        var conflicts = new List<FileConflict>();

        var existingLines = existingContent.Split('\n');
        var proposedLines = proposedContent.Split('\n');

        // Size-based conflict detection
        var sizeRatio = (double)existingLines.Length / Math.Max(proposedLines.Length, 1);
        if (sizeRatio < 0.3 || sizeRatio > 3.0)
        {
            conflicts.Add(new FileConflict
            {
                StartLine = 1,
                EndLine = existingLines.Length,
                Description = $"File structure differs significantly (existing: {existingLines.Length} lines, proposed: {proposedLines.Length} lines)",
                Severity = ConflictSeverity.High
            });
            return conflicts;
        }

        // Block-based analysis: detect regions that differ
        var diffRegions = FindDiffRegions(existingLines, proposedLines);
        foreach (var region in diffRegions)
        {
            var existingSlice = string.Join('\n', existingLines[region.start..Math.Min(region.start + 5, existingLines.Length)]);
            var proposedSlice = string.Join('\n', proposedLines[region.start..Math.Min(region.start + 5, proposedLines.Length)]);

            var severity = region.length > 10 ? ConflictSeverity.High :
                           region.length > 3 ? ConflictSeverity.Medium : ConflictSeverity.Low;

            conflicts.Add(new FileConflict
            {
                StartLine = region.start + 1,
                EndLine = Math.Min(region.end + 1, existingLines.Length),
                Description = $"Code block differs at lines {region.start + 1}-{Math.Min(region.end + 1, existingLines.Length)}",
                Severity = severity
            });
        }

        return conflicts;
    }

    public string? TryAutoMerge(string existingContent, string proposedContent, FileChange change)
    {
        if (change.Conflicts.Count > 0)
            return null;

        return proposedContent;
    }

    public MergeStrategy DetermineStrategy(FileChange change)
    {
        if (change.Type == ChangeType.New) return MergeStrategy.AcceptProposed;
        if (change.Type == ChangeType.Unchanged) return MergeStrategy.KeepExisting;
        if (change.Conflicts.Count == 0) return MergeStrategy.AcceptProposed;

        return change.Conflicts.Any(c => c.Severity == ConflictSeverity.High)
            ? MergeStrategy.ManualResolution
            : MergeStrategy.AcceptProposedWithReview;
    }

    private static List<(int start, int end, int length)> FindDiffRegions(
        string[] existingLines, string[] proposedLines)
    {
        var regions = new List<(int start, int end, int length)>();
        var maxLen = Math.Max(existingLines.Length, proposedLines.Length);
        int? regionStart = null;

        for (var i = 0; i < maxLen; i++)
        {
            var eLine = i < existingLines.Length ? existingLines[i].TrimEnd('\r') : null;
            var pLine = i < proposedLines.Length ? proposedLines[i].TrimEnd('\r') : null;

            if (eLine != pLine)
            {
                regionStart ??= i;
            }
            else if (regionStart.HasValue)
            {
                regions.Add((regionStart.Value, i, i - regionStart.Value));
                regionStart = null;
            }
        }

        if (regionStart.HasValue)
            regions.Add((regionStart.Value, maxLen, maxLen - regionStart.Value));

        return regions;
    }
}

public enum MergeStrategy
{
    KeepExisting,
    AcceptProposed,
    AcceptProposedWithReview,
    ManualResolution
}
