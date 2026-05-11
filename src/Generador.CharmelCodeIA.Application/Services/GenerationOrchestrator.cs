using Generador.CharmelCodeIA.Application.UseCases.AnalyzeDifferential;
using Generador.CharmelCodeIA.Application.UseCases.GenerateEntities;
using Generador.CharmelCodeIA.Application.UseCases.GenerateUseCases;
using Generador.CharmelCodeIA.Application.UseCases.ReadDatabaseSchema;
using Generador.CharmelCodeIA.Application.UseCases.ScaffoldProject;
using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.Services;

public sealed class GenerationOrchestrator
{
    private readonly IMediator _mediator;
    private readonly SchemaAnalyzer _schemaAnalyzer;
    private readonly ILogger<GenerationOrchestrator> _logger;

    public GenerationOrchestrator(
        IMediator mediator,
        SchemaAnalyzer schemaAnalyzer,
        ILogger<GenerationOrchestrator> logger)
    {
        _mediator = mediator;
        _schemaAnalyzer = schemaAnalyzer;
        _logger = logger;
    }

    public async Task<GenerationPipelineResult> ExecuteFullGenerationAsync(
        string connectionString,
        DatabaseProviderType provider,
        string companyName,
        string projectName,
        string outputPath,
        List<UseCaseDefinition>? useCases = null,
        CancellationToken cancellationToken = default)
    {
        var steps = new List<GenerationStep>();

        _logger.LogInformation("Starting full generation pipeline for {Company}.{Project}", companyName, projectName);

        // Step 1: Read database schema
        steps.Add(new GenerationStep { Name = "Read Schema", Status = StepStatus.InProgress });
        var schemaResult = await _mediator.Send(new ReadDatabaseSchemaCommand
        {
            ConnectionString = connectionString,
            Provider = provider
        }, cancellationToken);

        if (!schemaResult.Success || schemaResult.Schema == null)
        {
            steps[0].Status = StepStatus.Failed;
            steps[0].Error = schemaResult.ErrorMessage;
            return new GenerationPipelineResult { Steps = steps, Success = false };
        }
        steps[0].Status = StepStatus.Completed;
        _logger.LogInformation("Schema read: {Tables} tables", schemaResult.Schema.Tables.Count);

        // Step 2: Detect value objects
        steps.Add(new GenerationStep { Name = "Analyze Schema", Status = StepStatus.InProgress });
        var valueObjects = _schemaAnalyzer.DetectValueObjects(schemaResult.Schema);
        steps[1].Status = StepStatus.Completed;
        steps[1].Metadata["ValueObjectCount"] = valueObjects.Count.ToString();

        // Step 3: Generate entities
        steps.Add(new GenerationStep { Name = "Generate Entities", Status = StepStatus.InProgress });
        var entitiesResult = await _mediator.Send(new GenerateEntitiesCommand
        {
            Schema = schemaResult.Schema,
            CompanyName = companyName,
            ProjectName = projectName,
            OutputPath = outputPath
        }, cancellationToken);

        if (!entitiesResult.Success)
        {
            steps[2].Status = StepStatus.Failed;
            steps[2].Error = string.Join("; ", entitiesResult.Errors);
            return new GenerationPipelineResult { Steps = steps, Success = false };
        }
        steps[2].Status = StepStatus.Completed;
        steps[2].Metadata["EntityCount"] = entitiesResult.GeneratedFiles.Count.ToString();
        _logger.LogInformation("Generated {Count} entity/config files", entitiesResult.GeneratedFiles.Count);

        // Step 4: Scaffold project
        steps.Add(new GenerationStep { Name = "Scaffold Project", Status = StepStatus.InProgress });
        var scaffoldResult = await _mediator.Send(new ScaffoldProjectCommand
        {
            CompanyName = companyName,
            ProjectName = projectName,
            OutputPath = outputPath,
            GeneratedFiles = entitiesResult.GeneratedFiles
        }, cancellationToken);

        if (!scaffoldResult.Success)
        {
            steps[3].Status = StepStatus.Failed;
            steps[3].Error = scaffoldResult.ErrorMessage;
            return new GenerationPipelineResult { Steps = steps, Success = false };
        }
        steps[3].Status = StepStatus.Completed;
        steps[3].Metadata["FileCount"] = scaffoldResult.CreatedFiles.Count.ToString();
        _logger.LogInformation("Scaffolded {Count} files", scaffoldResult.CreatedFiles.Count);

        // Step 5: Generate use cases (optional)
        if (useCases?.Any() == true)
        {
            steps.Add(new GenerationStep { Name = "Generate Use Cases", Status = StepStatus.InProgress });
            var useCaseResult = await _mediator.Send(new GenerateUseCasesCommand
            {
                UseCases = useCases,
                Schema = schemaResult.Schema,
                CompanyName = companyName,
                ProjectName = projectName
            }, cancellationToken);

            // Write use case files
            foreach (var (key, files) in useCaseResult.GeneratedFiles)
            {
                foreach (var (relativePath, content) in files)
                {
                    var fullPath = Path.Combine(outputPath, "src", relativePath);
                    var dir = Path.GetDirectoryName(fullPath);
                    if (dir != null)
                    {
                        Directory.CreateDirectory(dir);
                        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
                    }
                }
            }

            steps[4].Status = useCaseResult.Success ? StepStatus.Completed : StepStatus.CompletedWithWarnings;
            steps[4].Metadata["UseCaseCount"] = useCaseResult.GeneratedFiles.Count.ToString();
        }

        return new GenerationPipelineResult
        {
            Success = true,
            Steps = steps,
            OutputPath = outputPath,
            SolutionPath = scaffoldResult.SolutionPath
        };
    }

    public async Task<GenerationPipelineResult> ExecuteIncrementalAsync(
        string connectionString,
        DatabaseProviderType provider,
        string existingProjectPath,
        string companyName,
        string projectName,
        List<UseCaseDefinition>? useCases = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting incremental generation for {Path}", existingProjectPath);

        var schemaResult = await _mediator.Send(new ReadDatabaseSchemaCommand
        {
            ConnectionString = connectionString,
            Provider = provider
        }, cancellationToken);

        if (!schemaResult.Success || schemaResult.Schema == null)
        {
            return new GenerationPipelineResult { Success = false };
        }

        var entitiesResult = await _mediator.Send(new GenerateEntitiesCommand
        {
            Schema = schemaResult.Schema,
            CompanyName = companyName,
            ProjectName = projectName,
            OutputPath = existingProjectPath
        }, cancellationToken);

        var differential = await _mediator.Send(new AnalyzeDifferentialCommand
        {
            ProjectPath = existingProjectPath,
            ProposedFiles = entitiesResult.GeneratedFiles
        }, cancellationToken);

        return new GenerationPipelineResult
        {
            Success = differential.Success,
            Differential = differential.Differential,
            Steps = new List<GenerationStep>
            {
                new() { Name = "Read Schema", Status = StepStatus.Completed },
                new() { Name = "Generate Entities", Status = StepStatus.Completed },
                new() { Name = "Analyze Differential", Status = StepStatus.Completed }
            }
        };
    }
}

public sealed class GenerationPipelineResult
{
    public bool Success { get; set; }
    public List<GenerationStep> Steps { get; set; } = new();
    public string? OutputPath { get; set; }
    public string? SolutionPath { get; set; }
    public DifferentialResult? Differential { get; set; }
}

public sealed class GenerationStep
{
    public string Name { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum StepStatus
{
    Pending,
    InProgress,
    Completed,
    CompletedWithWarnings,
    Failed,
    Skipped
}
