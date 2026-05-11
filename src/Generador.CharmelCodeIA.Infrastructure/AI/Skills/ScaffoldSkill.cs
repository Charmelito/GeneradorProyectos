using System.ComponentModel;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Generador.CharmelCodeIA.Infrastructure.AI.Skills;

public sealed class ScaffoldSkill
{
    private readonly IServiceProvider _serviceProvider;

    public ScaffoldSkill(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [KernelFunction("scaffold_full_solution")]
    [Description("Generates a complete Clean Architecture solution structure with all layers")]
    [return: Description("Summary of created files and folders")]
    public async Task<string> ScaffoldFullSolutionAsync(
        [Description("Company name")] string companyName,
        [Description("Project name")] string projectName,
        [Description("Output directory path")] string outputPath,
        [Description("Target framework (e.g., net10.0)")] string targetFramework = "net10.0")
    {
        var scaffolder = _serviceProvider.GetRequiredService<IProjectScaffolder>();
        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(companyName, projectName),
            OutputPath = outputPath,
            TargetFramework = targetFramework,
            LayersToGenerate = new[] { Domain.Enums.ProjectLayer.Domain, Domain.Enums.ProjectLayer.Application, Domain.Enums.ProjectLayer.Infrastructure, Domain.Enums.ProjectLayer.WebApi }
        };

        await scaffolder.ScaffoldFullSolutionAsync(config);
        var files = await scaffolder.GetGeneratedFilesAsync(outputPath);
        return $"Generated {files.Count} files in {outputPath}";
    }

    [KernelFunction("scaffold_incremental")]
    [Description("Adds incremental files to an existing project based on a differential analysis")]
    [return: Description("Summary of added/modified files")]
    public async Task<string> ScaffoldIncrementalAsync(
        [Description("Path to the existing project")] string existingProjectPath,
        [Description("JSON of DifferentialResult")] string differentialJson)
    {
        var scaffolder = _serviceProvider.GetRequiredService<IProjectScaffolder>();
        var differential = System.Text.Json.JsonSerializer.Deserialize<DifferentialResult>(differentialJson)
            ?? new DifferentialResult();

        var config = new ProjectConfiguration
        {
            ExistingProjectPath = existingProjectPath,
            OutputPath = existingProjectPath,
            Naming = new Domain.ValueObjects.NamingConvention("Temp", "Temp")
        };

        await scaffolder.ScaffoldIncrementalAsync(config, differential);
        return $"Applied {differential.Summary.TotalChanges} changes";
    }

    [KernelFunction("validate_structure")]
    [Description("Validates that the output directory has the expected Clean Architecture structure")]
    [return: Description("Validation result with any issues found")]
    public string ValidateStructure(
        [Description("Output directory path")] string outputPath)
    {
        var expectedFolders = new[]
        {
            "src", $"src/*.Domain", $"src/*.Application",
            $"src/*.Infrastructure", $"src/*.WebApi"
        };

        var existingDirs = Directory.Exists(outputPath)
            ? Directory.GetDirectories(outputPath, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();

        var issues = new List<string>();

        if (!Directory.Exists(outputPath))
            return "ERROR: Output directory does not exist";

        if (!existingDirs.Any(d => d.Contains(".Domain")))
            issues.Add("Missing Domain project");
        if (!existingDirs.Any(d => d.Contains(".Application")))
            issues.Add("Missing Application project");
        if (!existingDirs.Any(d => d.Contains(".Infrastructure")))
            issues.Add("Missing Infrastructure project");

        return issues.Count == 0
            ? "Structure validation passed"
            : "Issues found:\n" + string.Join("\n", issues);
    }
}
