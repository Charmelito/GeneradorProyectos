namespace Generador.CharmelCodeIA.Domain.ValueObjects;

public sealed class NamingConvention : ValueObject
{
    public string CompanyName { get; }
    public string ProjectName { get; }

    public NamingConvention(string companyName, string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        CompanyName = Sanitize(companyName);
        ProjectName = Sanitize(projectName);
    }

    public string SolutionName => $"{CompanyName}.{ProjectName}";

    public string GetProjectName(Enums.ProjectLayer layer) => layer switch
    {
        Enums.ProjectLayer.Domain => $"{CompanyName}.{ProjectName}.Domain",
        Enums.ProjectLayer.Application => $"{CompanyName}.{ProjectName}.Application",
        Enums.ProjectLayer.Infrastructure => $"{CompanyName}.{ProjectName}.Infrastructure",
        Enums.ProjectLayer.Presentation => $"{CompanyName}.{ProjectName}.Presentation",
        Enums.ProjectLayer.WebApi => $"{CompanyName}.{ProjectName}.WebApi",
        Enums.ProjectLayer.Shared => $"{CompanyName}.{ProjectName}.Shared",
        _ => throw new ArgumentOutOfRangeException(nameof(layer))
    };

    public string GetRootNamespace(Enums.ProjectLayer layer) => GetProjectName(layer);

    public string GetEntityNamespace() => $"{CompanyName}.{ProjectName}.Domain.Entities";

    public string GetValueObjectNamespace() => $"{CompanyName}.{ProjectName}.Domain.ValueObjects";

    public string GetUseCaseNamespace(string entityName, string action) =>
        $"{CompanyName}.{ProjectName}.Application.{entityName}.{action}";

    public string GetRepositoryNamespace() =>
        $"{CompanyName}.{ProjectName}.Domain.Interfaces";

    public string GetDbContextNamespace() =>
        $"{CompanyName}.{ProjectName}.Infrastructure.Persistence";

    private static string Sanitize(string input) =>
        new string(input.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '_').ToArray());

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CompanyName.ToLowerInvariant();
        yield return ProjectName.ToLowerInvariant();
    }

    public override string ToString() => SolutionName;
}
