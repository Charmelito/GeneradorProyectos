using Generador.CharmelCodeIA.Domain.Entities;
using Generador.CharmelCodeIA.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.UseCases.AnalyzeDifferential;

public sealed class AnalyzeDifferentialHandler : IRequestHandler<AnalyzeDifferentialCommand, AnalyzeDifferentialResult>
{
    private readonly IDifferentialAnalyzer _differentialAnalyzer;
    private readonly ILogger<AnalyzeDifferentialHandler> _logger;

    public AnalyzeDifferentialHandler(
        IDifferentialAnalyzer differentialAnalyzer,
        ILogger<AnalyzeDifferentialHandler> logger)
    {
        _differentialAnalyzer = differentialAnalyzer;
        _logger = logger;
    }

    public async Task<AnalyzeDifferentialResult> Handle(
        AnalyzeDifferentialCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _differentialAnalyzer.AnalyzeAsync(
                request.ProjectPath, request.ProposedFiles, cancellationToken);

            _logger.LogInformation(
                "Differential analysis complete: {New} new, {Modified} modified, {Conflicts} conflicts",
                result.Summary.NewFiles, result.Summary.ModifiedFiles, result.Summary.ConflictFiles);

            return new AnalyzeDifferentialResult
            {
                Success = true,
                Differential = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze differential");
            return new AnalyzeDifferentialResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
