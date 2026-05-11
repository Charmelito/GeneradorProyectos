using Generador.CharmelCodeIA.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.UseCases.ConfirmGeneration;

public sealed class ConfirmGenerationHandler : IRequestHandler<ConfirmGenerationCommand, ConfirmGenerationResult>
{
    private readonly ILogger<ConfirmGenerationHandler> _logger;

    public ConfirmGenerationHandler(ILogger<ConfirmGenerationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ConfirmGenerationResult> Handle(
        ConfirmGenerationCommand request, CancellationToken cancellationToken)
    {
        var filesWritten = 0;
        var errors = new List<string>();

        try
        {
            var changesToApply = request.ApplyAll
                ? request.Differential.Changes
                : request.Differential.Changes
                    .Where(c => request.ConfirmedFiles.Contains(c.RelativePath))
                    .ToList();

            foreach (var change in changesToApply)
            {
                try
                {
                    var fullPath = Path.Combine(request.OutputPath, change.RelativePath);
                    var dir = Path.GetDirectoryName(fullPath);

                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    await File.WriteAllTextAsync(fullPath, change.ProposedContent, cancellationToken);
                    change.IsConfirmed = true;
                    filesWritten++;

                    _logger.LogInformation("Wrote file: {Path}", change.RelativePath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to write {change.RelativePath}: {ex.Message}");
                    _logger.LogError(ex, "Failed to write file {Path}", change.RelativePath);
                }
            }

            return new ConfirmGenerationResult
            {
                Success = errors.Count == 0,
                FilesWritten = filesWritten,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm generation");
            return new ConfirmGenerationResult
            {
                Success = false,
                Errors = new[] { ex.Message }
            };
        }
    }
}
