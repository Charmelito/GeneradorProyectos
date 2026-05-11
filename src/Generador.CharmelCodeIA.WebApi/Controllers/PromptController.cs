using Generador.CharmelCodeIA.Application.UseCases.ManagePrompts;
using Generador.CharmelCodeIA.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Generador.CharmelCodeIA.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PromptController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromptController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category)
    {
        var prompts = await _mediator.Send(new GetPromptsQuery { Category = category });
        return Ok(prompts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var prompts = await _mediator.Send(new GetPromptsQuery());
        var prompt = prompts.FirstOrDefault(p => p.Id == id);
        return prompt is not null ? Ok(prompt) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PromptTemplate prompt)
    {
        prompt.Id = prompt.Id == Guid.Empty ? Guid.NewGuid() : prompt.Id;
        prompt.UpdatedAt = DateTime.UtcNow;

        if (prompt.CreatedAt == default)
            prompt.CreatedAt = DateTime.UtcNow;

        var success = await _mediator.Send(new SavePromptCommand { Prompt = prompt });
        return success ? Ok(new { id = prompt.Id }) : BadRequest(new { error = "Failed to save prompt" });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PromptTemplate prompt)
    {
        prompt.Id = id;
        prompt.UpdatedAt = DateTime.UtcNow;
        var success = await _mediator.Send(new SavePromptCommand { Prompt = prompt });
        return success ? Ok(new { id }) : BadRequest(new { error = "Failed to update prompt" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _mediator.Send(new DeletePromptCommand { Id = id });
        return success ? NoContent() : NotFound();
    }
}
