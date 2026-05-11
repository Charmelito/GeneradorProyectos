using Generador.CharmelCodeIA.Domain.Interfaces;
using MediatR;

namespace Generador.CharmelCodeIA.Application.UseCases.ManagePrompts;

public sealed record GetPromptsQuery : IRequest<IReadOnlyList<PromptTemplate>>
{
    public string? Category { get; init; }
}

public sealed record SavePromptCommand : IRequest<bool>
{
    public PromptTemplate Prompt { get; init; } = null!;
}

public sealed record DeletePromptCommand : IRequest<bool>
{
    public Guid Id { get; init; }
}
