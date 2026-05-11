using Generador.CharmelCodeIA.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.UseCases.ManagePrompts;

public sealed class ManagePromptsHandlers :
    IRequestHandler<GetPromptsQuery, IReadOnlyList<PromptTemplate>>,
    IRequestHandler<SavePromptCommand, bool>,
    IRequestHandler<DeletePromptCommand, bool>
{
    private readonly IPromptRepository _repository;
    private readonly ILogger<ManagePromptsHandlers> _logger;

    public ManagePromptsHandlers(IPromptRepository repository, ILogger<ManagePromptsHandlers> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PromptTemplate>> Handle(
        GetPromptsQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Category))
            return await _repository.GetByCategoryAsync(request.Category, cancellationToken);

        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<bool> Handle(
        SavePromptCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(request.Prompt, cancellationToken);
            _logger.LogInformation("Prompt saved: {Name}", request.Prompt.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save prompt {Name}", request.Prompt.Name);
            return false;
        }
    }

    public async Task<bool> Handle(
        DeletePromptCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.DeleteAsync(request.Id, cancellationToken);
            _logger.LogInformation("Prompt deleted: {Id}", request.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete prompt {Id}", request.Id);
            return false;
        }
    }
}
