namespace Generador.CharmelCodeIA.Domain.Entities;

public class DifferentialResult
{
    public string ProjectPath { get; set; } = string.Empty;
    public List<FileChange> Changes { get; set; } = new();
    public DifferentialSummary Summary { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class FileChange
{
    public string RelativePath { get; set; } = string.Empty;
    public ChangeType Type { get; set; }
    public string ExistingContent { get; set; } = string.Empty;
    public string ProposedContent { get; set; } = string.Empty;
    public string Diff { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public List<FileConflict> Conflicts { get; set; } = new();
}

public class FileConflict
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Description { get; set; } = string.Empty;
    public ConflictSeverity Severity { get; set; }
}

public enum ChangeType
{
    New = 1,
    Modified = 2,
    Unchanged = 3,
    Conflict = 4
}

public enum ConflictSeverity
{
    Low = 1,
    Medium = 2,
    High = 3
}

public class DifferentialSummary
{
    public int NewFiles { get; set; }
    public int ModifiedFiles { get; set; }
    public int UnchangedFiles { get; set; }
    public int ConflictFiles { get; set; }
    public int TotalChanges => NewFiles + ModifiedFiles + ConflictFiles;
}
