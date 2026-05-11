using Generador.CharmelCodeIA.Domain.Enums;
using Generador.CharmelCodeIA.Domain.ValueObjects;

namespace Generador.CharmelCodeIA.Domain.Entities;

public class ProjectConfiguration
{
    public NamingConvention Naming { get; set; } = null!;
    public string OutputPath { get; set; } = string.Empty;
    public GenerationMode Mode { get; set; } = GenerationMode.FullSolution;
    public IReadOnlyList<ProjectLayer> LayersToGenerate { get; set; } = Array.Empty<ProjectLayer>();
    public bool GenerateUnitTestsProject { get; set; } = true;
    public bool GenerateIntegrationTestsProject { get; set; } = false;
    public bool IncludeSwagger { get; set; } = true;
    public bool IncludeSerilog { get; set; } = true;
    public bool IncludeMediatR { get; set; } = true;
    public bool IncludeFluentValidation { get; set; } = true;
    public string TargetFramework { get; set; } = "net10.0";
    public string ExistingProjectPath { get; set; } = string.Empty;
}
