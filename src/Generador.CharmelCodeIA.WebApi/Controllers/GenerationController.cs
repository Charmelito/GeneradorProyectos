using System.ComponentModel.DataAnnotations;
using Generador.CharmelCodeIA.Application.Services;
using Generador.CharmelCodeIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Generador.CharmelCodeIA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GenerationController : ControllerBase
{
    private readonly GenerationOrchestrator _orchestrator;

    public GenerationController(GenerationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("full")]
    public async Task<IActionResult> GenerateFull([FromBody] FullGenerationRequest request)
    {
        var useCases = request.UseCases?.Select(uc => new UseCaseDefinition
        {
            EntityName = uc.EntityName,
            Action = uc.Action,
            Type = uc.Type,
            Description = uc.Description ?? $"{uc.Action} {uc.EntityName}"
        }).ToList();

        var result = await _orchestrator.ExecuteFullGenerationAsync(
            request.ConnectionString,
            request.Provider,
            request.CompanyName,
            request.ProjectName,
            request.OutputPath,
            useCases);

        return result.Success
            ? Ok(new
            {
                outputPath = result.OutputPath,
                solutionPath = result.SolutionPath,
                steps = result.Steps.Select(s => new
                {
                    s.Name,
                    Status = s.Status.ToString(),
                    s.Error,
                    s.Metadata
                })
            })
            : BadRequest(new
            {
                error = "Generation pipeline failed",
                steps = result.Steps.Where(s => s.Status == StepStatus.Failed)
                    .Select(s => new { s.Name, s.Error })
            });
    }

    [HttpPost("incremental")]
    public async Task<IActionResult> GenerateIncremental([FromBody] IncrementalGenerationRequest request)
    {
        var result = await _orchestrator.ExecuteIncrementalAsync(
            request.ConnectionString,
            request.Provider,
            request.ExistingProjectPath,
            request.CompanyName,
            request.ProjectName);

        return Ok(new
        {
            success = result.Success,
            differential = result.Differential
        });
    }
}

public sealed class FullGenerationRequest
{
    [Required] public string ConnectionString { get; set; } = string.Empty;
    [Required] public Domain.Enums.DatabaseProviderType Provider { get; set; }
    [Required] public string CompanyName { get; set; } = string.Empty;
    [Required] public string ProjectName { get; set; } = string.Empty;
    [Required] public string OutputPath { get; set; } = string.Empty;
    public List<UseCaseRequest>? UseCases { get; set; }
}

public sealed class IncrementalGenerationRequest
{
    [Required] public string ConnectionString { get; set; } = string.Empty;
    [Required] public Domain.Enums.DatabaseProviderType Provider { get; set; }
    [Required] public string ExistingProjectPath { get; set; } = string.Empty;
    [Required] public string CompanyName { get; set; } = string.Empty;
    [Required] public string ProjectName { get; set; } = string.Empty;
}

public sealed class UseCaseRequest
{
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public UseCaseType Type { get; set; }
    public string? Description { get; set; }
}
