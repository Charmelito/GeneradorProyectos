using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using Generador.CharmelCodeIA.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.UseCases.GenerateEntities;

public sealed class GenerateEntitiesHandler : IRequestHandler<GenerateEntitiesCommand, GenerateEntitiesResult>
{
    private readonly IEntityGenerator _entityGenerator;
    private readonly ILogger<GenerateEntitiesHandler> _logger;

    public GenerateEntitiesHandler(IEntityGenerator entityGenerator, ILogger<GenerateEntitiesHandler> logger)
    {
        _entityGenerator = entityGenerator;
        _logger = logger;
    }

    public async Task<GenerateEntitiesResult> Handle(
        GenerateEntitiesCommand request, CancellationToken cancellationToken)
    {
        var files = new Dictionary<string, string>();
        var errors = new List<string>();

        try
        {
            var config = new ProjectConfiguration
            {
                Naming = new NamingConvention(request.CompanyName, request.ProjectName),
                OutputPath = request.OutputPath
            };

            foreach (var table in request.Schema.Tables)
            {
                try
                {
                    var entityCode = await _entityGenerator.GenerateEntityAsync(table, config, cancellationToken);
                    var fileName = $"Domain/Entities/{table.Name}.cs";
                    files[fileName] = entityCode;

                    if (request.GenerateConfigurations)
                    {
                        var configCode = await _entityGenerator.GenerateConfigurationAsync(table, config, cancellationToken);
                        files[$"Infrastructure/Persistence/Configurations/{table.Name}Configuration.cs"] = configCode;
                    }

                    _logger.LogInformation("Generated entity for table {TableName}", table.Name);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to generate entity for {table.Name}: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate entity for {TableName}", table.Name);
                }
            }

            if (request.GenerateDbContext && request.Schema.Tables.Any())
            {
                try
                {
                    var dbContextCode = await _entityGenerator.GenerateDbContextAsync(
                        request.Schema.Tables, config, cancellationToken);
                    files["Infrastructure/Persistence/AppDbContext.cs"] = dbContextCode;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to generate DbContext: {ex.Message}");
                }
            }

            return new GenerateEntitiesResult
            {
                Success = errors.Count == 0,
                GeneratedFiles = files,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate entities");
            return new GenerateEntitiesResult
            {
                Success = false,
                Errors = new[] { ex.Message }
            };
        }
    }
}
