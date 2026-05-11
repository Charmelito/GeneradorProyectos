using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.UseCases.GenerateUseCases;

public sealed class GenerateUseCasesHandler : IRequestHandler<GenerateUseCasesCommand, GenerateUseCasesResult>
{
    private readonly IUseCaseGenerator _useCaseGenerator;
    private readonly ILogger<GenerateUseCasesHandler> _logger;

    public GenerateUseCasesHandler(
        IUseCaseGenerator useCaseGenerator,
        ILogger<GenerateUseCasesHandler> logger)
    {
        _useCaseGenerator = useCaseGenerator;
        _logger = logger;
    }

    public async Task<GenerateUseCasesResult> Handle(
        GenerateUseCasesCommand request, CancellationToken cancellationToken)
    {
        var allGenerated = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        var errors = new List<string>();

        var config = new ProjectConfiguration
        {
            Naming = new Domain.ValueObjects.NamingConvention(request.CompanyName, request.ProjectName)
        };

        foreach (var useCase in request.UseCases)
        {
            try
            {
                var files = new Dictionary<string, string>();

                var commandOrQuery = useCase.Type == UseCaseType.Command
                    ? await _useCaseGenerator.GenerateCommandAsync(useCase, request.Schema, config, cancellationToken)
                    : await _useCaseGenerator.GenerateQueryAsync(useCase, request.Schema, config, cancellationToken);

                var handler = await _useCaseGenerator.GenerateHandlerAsync(useCase, request.Schema, config, cancellationToken);
                var result = await _useCaseGenerator.GenerateResultAsync(useCase, request.Schema, config, cancellationToken);

                var folder = $"Application/{useCase.EntityName}/{useCase.Action}";
                var typeName = useCase.Type == UseCaseType.Command ? "Command" : "Query";

                files[$"{folder}/{useCase.Action}{useCase.EntityName}{typeName}.cs"] = commandOrQuery;
                files[$"{folder}/{useCase.Action}{useCase.EntityName}Handler.cs"] = handler;
                files[$"{folder}/{useCase.Action}{useCase.EntityName}Result.cs"] = result;

                allGenerated[$"{useCase.EntityName}/{useCase.Action}"] = files;

                _logger.LogInformation("Generated use case {Entity}/{Action}", useCase.EntityName, useCase.Action);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to generate {useCase.EntityName}/{useCase.Action}: {ex.Message}");
                _logger.LogError(ex, "Failed to generate use case {Entity}/{Action}", useCase.EntityName, useCase.Action);
            }
        }

        return new GenerateUseCasesResult
        {
            Success = errors.Count == 0,
            GeneratedFiles = allGenerated,
            Errors = errors
        };
    }
}
