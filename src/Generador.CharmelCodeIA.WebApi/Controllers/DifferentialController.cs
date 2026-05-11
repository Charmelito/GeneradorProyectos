using Generador.CharmelCodeIA.Application.UseCases.AnalyzeDifferential;
using Generador.CharmelCodeIA.Application.UseCases.ConfirmGeneration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Generador.CharmelCodeIA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DifferentialController : ControllerBase
{
    private readonly IMediator _mediator;

    public DifferentialController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
    {
        var result = await _mediator.Send(new AnalyzeDifferentialCommand
        {
            ProjectPath = request.ProjectPath,
            ProposedFiles = request.ProposedFiles
        });

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            projectPath = result.Differential!.ProjectPath,
            summary = new
            {
                result.Differential.Summary.NewFiles,
                result.Differential.Summary.ModifiedFiles,
                result.Differential.Summary.UnchangedFiles,
                result.Differential.Summary.ConflictFiles,
                result.Differential.Summary.TotalChanges
            },
            changes = result.Differential.Changes.Select(c => new
            {
                c.RelativePath,
                Type = c.Type.ToString(),
                c.Diff,
                c.IsConfirmed,
                conflicts = c.Conflicts.Select(fc => new
                {
                    fc.StartLine,
                    fc.EndLine,
                    fc.Description,
                    Severity = fc.Severity.ToString()
                })
            })
        });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest request)
    {
        var result = await _mediator.Send(new ConfirmGenerationCommand
        {
            Differential = request.Differential,
            OutputPath = request.OutputPath,
            ConfirmedFiles = request.ConfirmedFiles,
            ApplyAll = request.ApplyAll
        });

        return result.Success
            ? Ok(new { filesWritten = result.FilesWritten })
            : BadRequest(new { error = "Confirmation failed", result.Errors });
    }
}

public sealed class AnalyzeRequest
{
    public string ProjectPath { get; set; } = string.Empty;
    public Dictionary<string, string> ProposedFiles { get; set; } = new();
}

public sealed class ConfirmRequest
{
    public Domain.Entities.DifferentialResult Differential { get; set; } = null!;
    public string OutputPath { get; set; } = string.Empty;
    public List<string> ConfirmedFiles { get; set; } = new();
    public bool ApplyAll { get; set; }
}
