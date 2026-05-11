namespace Generador.CharmelCodeIA.WebUI.Models;

public sealed class TableNode
{
    public string Name { get; set; } = string.Empty;
    public int Columns { get; set; }
    public int ColumnCount { get; set; }
    public List<ColumnInfo> ColumnList { get; set; } = new();
}

public sealed class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string SqlType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
}

public sealed class StepInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class FileNode
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsDirectory { get; set; }
}
