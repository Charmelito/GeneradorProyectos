using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface IPromptRepository
{
    Task<PromptTemplate> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromptTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task SaveAsync(PromptTemplate prompt, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class PromptTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
