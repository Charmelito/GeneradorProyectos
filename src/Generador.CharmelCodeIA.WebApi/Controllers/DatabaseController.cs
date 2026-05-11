using System.ComponentModel.DataAnnotations;
using Generador.CharmelCodeIA.Application.UseCases.ReadDatabaseSchema;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Generador.CharmelCodeIA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DatabaseController : ControllerBase
{
    private readonly IMediator _mediator;

    public DatabaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] DatabaseConnectionRequest request)
    {
        var result = await _mediator.Send(new ReadDatabaseSchemaCommand
        {
            ConnectionString = request.ConnectionString,
            Provider = request.Provider,
            TestOnly = true
        });

        return result.Success
            ? Ok(new { connected = result.ConnectionValid })
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPost("read-schema")]
    public async Task<IActionResult> ReadSchema([FromBody] DatabaseConnectionRequest request)
    {
        var result = await _mediator.Send(new ReadDatabaseSchemaCommand
        {
            ConnectionString = request.ConnectionString,
            Provider = request.Provider
        });

        return result.Success
            ? Ok(new { schema = result.Schema, summary = result.Summary })
            : BadRequest(new { error = result.ErrorMessage });
    }
}

public sealed class DatabaseConnectionRequest
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public Domain.Enums.DatabaseProviderType Provider { get; set; }
}
