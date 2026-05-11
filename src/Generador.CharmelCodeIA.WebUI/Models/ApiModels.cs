using System.Text.Json.Serialization;

namespace Generador.CharmelCodeIA.WebUI.Models;

public sealed class ConnectionTestResult
{
    public bool Connected { get; set; }
    public string? Error { get; set; }
}

public sealed class SchemaResult
{
    public object? Schema { get; set; }
    public string? Summary { get; set; }
    public string? Error { get; set; }
}

public sealed class FullGenRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Provider { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public List<UseCaseDto>? UseCases { get; set; }
}

public sealed class IncGenRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Provider { get; set; }
    public string ExistingProjectPath { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
}

public sealed class UseCaseDto
{
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int Type { get; set; }
    public string? Description { get; set; }
}

public sealed class GenerationResult
{
    public string? OutputPath { get; set; }
    public string? SolutionPath { get; set; }
    public List<StepDto>? Steps { get; set; }
    public string? Error { get; set; }
}

public sealed class StepDto
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class IncrementalResult
{
    public bool Success { get; set; }
    public object? Differential { get; set; }
}

public sealed class PromptDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class DiffResult
{
    public string? ProjectPath { get; set; }
    public DiffSummary? Summary { get; set; }
    public List<DiffChange>? Changes { get; set; }
}

public sealed class DiffSummary
{
    public int NewFiles { get; set; }
    public int ModifiedFiles { get; set; }
    public int UnchangedFiles { get; set; }
    public int ConflictFiles { get; set; }
    public int TotalChanges { get; set; }
}

public sealed class DiffChange
{
    public string? RelativePath { get; set; }
    public string? Type { get; set; }
    public string? Diff { get; set; }
    public bool IsConfirmed { get; set; }
    public List<object>? Conflicts { get; set; }
}

public sealed class ConfirmRequest
{
    public object? Differential { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public List<string> ConfirmedFiles { get; set; } = new();
    public bool ApplyAll { get; set; }
}

public sealed class ConfirmResult
{
    public int FilesWritten { get; set; }
    public List<string>? Errors { get; set; }
}

public sealed class ProjectFilesResult
{
    public string? ProjectPath { get; set; }
    public List<FileInfoDto>? Files { get; set; }
}

public sealed class FileInfoDto
{
    public string? RelativePath { get; set; }
    public string? FullPath { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}

public sealed class FileContentResult
{
    public string? RelativePath { get; set; }
    public string? Content { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}
