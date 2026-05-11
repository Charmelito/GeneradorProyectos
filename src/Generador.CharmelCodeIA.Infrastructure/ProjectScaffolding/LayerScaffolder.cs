using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.ValueObjects;

namespace Generador.CharmelCodeIA.Infrastructure.ProjectScaffolding;

public sealed class LayerScaffolder
{
    public IReadOnlyList<string> CreateLayerStructure(
        string solutionDir, NamingConvention naming, ProjectLayer layer)
    {
        var projectDir = Path.Combine(solutionDir, "src", naming.GetProjectName(layer));
        var createdDirs = new List<string>();

        var folders = GetStandardFolders(layer);
        foreach (var folder in folders)
        {
            var fullPath = Path.Combine(projectDir, folder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                createdDirs.Add(fullPath);
            }
        }

        return createdDirs;
    }

    private static string[] GetStandardFolders(ProjectLayer layer) => layer switch
    {
        ProjectLayer.Domain => new[]
        {
            "Entities",
            "ValueObjects",
            "Interfaces",
            "Enums",
            "Exceptions"
        },
        ProjectLayer.Application => new[]
        {
            "Common\\Interfaces",
            "Common\\Behaviors",
            "Common\\Mappings",
            "Common\\Exceptions"
        },
        ProjectLayer.Infrastructure => new[]
        {
            "Persistence\\Configurations",
            "Persistence\\Repositories",
            "Persistence\\Migrations",
            "Services",
            "DependencyInjection"
        },
        ProjectLayer.WebApi => new[]
        {
            "Controllers",
            "Middleware",
            "Filters",
            "Models"
        },
        _ => Array.Empty<string>()
    };
}
