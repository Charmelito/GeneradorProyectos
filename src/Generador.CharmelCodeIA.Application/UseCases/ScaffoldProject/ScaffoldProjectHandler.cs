using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.UseCases.ScaffoldProject;

public sealed class ScaffoldProjectHandler : IRequestHandler<ScaffoldProjectCommand, ScaffoldProjectResult>
{
    private readonly IProjectScaffolder _projectScaffolder;
    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<ScaffoldProjectHandler> _logger;

    public ScaffoldProjectHandler(
        IProjectScaffolder projectScaffolder,
        ITemplateEngine templateEngine,
        ILogger<ScaffoldProjectHandler> logger)
    {
        _projectScaffolder = projectScaffolder;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task<ScaffoldProjectResult> Handle(
        ScaffoldProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var config = new ProjectConfiguration
            {
                Naming = new Domain.ValueObjects.NamingConvention(request.CompanyName, request.ProjectName),
                OutputPath = request.OutputPath,
                Mode = request.Mode,
                LayersToGenerate = request.Layers.Any() ? request.Layers : new[]
                {
                    Domain.Enums.ProjectLayer.Domain,
                    Domain.Enums.ProjectLayer.Application,
                    Domain.Enums.ProjectLayer.Infrastructure,
                    Domain.Enums.ProjectLayer.WebApi
                }
            };

            if (request.Mode == Domain.Enums.GenerationMode.IncrementalUpdate && request.Differential != null)
            {
                await _projectScaffolder.ScaffoldIncrementalAsync(config, request.Differential, cancellationToken);
            }
            else
            {
                await _projectScaffolder.ScaffoldFullSolutionAsync(config, cancellationToken);

                foreach (var (relativePath, content) in request.GeneratedFiles)
                {
                    var fullPath = Path.Combine(config.OutputPath, "src", relativePath);
                    var dir = Path.GetDirectoryName(fullPath);
                    if (dir != null)
                    {
                        Directory.CreateDirectory(dir);
                        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
                    }
                }
            }

            var generatedFiles = await _projectScaffolder.GetGeneratedFilesAsync(request.OutputPath);

            _logger.LogInformation("Project scaffolded: {FileCount} files in {Path}",
                generatedFiles.Count, request.OutputPath);

            return new ScaffoldProjectResult
            {
                Success = true,
                SolutionPath = Path.Combine(request.OutputPath,
                    $"{config.Naming.SolutionName}.sln"),
                CreatedFiles = generatedFiles
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scaffold project");
            return new ScaffoldProjectResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
