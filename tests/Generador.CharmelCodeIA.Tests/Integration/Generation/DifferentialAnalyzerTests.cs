using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Infrastructure.DifferentialAnalysis;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Tests.Integration.Generation;

public sealed class DifferentialAnalyzerTests
{
    private readonly ConflictResolver _conflictResolver = new();
    private readonly ProjectFileScanner _fileScanner;
    private readonly DifferentialAnalyzer _analyzer;

    public DifferentialAnalyzerTests()
    {
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        _fileScanner = new ProjectFileScanner(loggerFactory.CreateLogger<ProjectFileScanner>());
        _analyzer = new DifferentialAnalyzer(_fileScanner, _conflictResolver, loggerFactory.CreateLogger<DifferentialAnalyzer>());
    }

    [Fact]
    public void CompareFile_IdenticalContent_ReturnsUnchanged()
    {
        var change = _analyzer.CompareFile("test.cs", "content", "content");

        Assert.Equal(ChangeType.Unchanged, change.Type);
        Assert.True(change.IsConfirmed);
    }

    [Fact]
    public void CompareFile_DifferentContent_ReturnsModified()
    {
        var change = _analyzer.CompareFile("test.cs", "original", "modified");

        Assert.True(change.Type is ChangeType.Modified or ChangeType.Conflict);
    }

    [Fact]
    public void ConflictResolver_IdenticalFiles_NoConflicts()
    {
        var conflicts = _conflictResolver.DetectConflicts("line1\nline2\nline3", "line1\nline2\nline3");

        Assert.Empty(conflicts);
    }

    [Fact]
    public void ConflictResolver_DifferentFiles_DetectsConflicts()
    {
        var conflicts = _conflictResolver.DetectConflicts("line1\nline2\nline3\nline4", "line1\nmodified\nline3\nline4");

        Assert.NotEmpty(conflicts);
    }
}
