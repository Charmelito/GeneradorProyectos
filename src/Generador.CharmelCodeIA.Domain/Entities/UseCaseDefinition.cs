namespace Generador.CharmelCodeIA.Domain.Entities;

public class UseCaseDefinition
{
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public UseCaseType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PromptTemplateId { get; set; } = string.Empty;
    public Dictionary<string, string> CustomParameters { get; set; } = new();
}

public enum UseCaseType
{
    Command = 1,
    Query = 2
}
