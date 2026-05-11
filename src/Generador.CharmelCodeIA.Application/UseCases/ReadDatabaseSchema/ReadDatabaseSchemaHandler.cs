using Generador.CharmelCodeIA.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Generador.CharmelCodeIA.Application.UseCases.ReadDatabaseSchema;

public sealed class ReadDatabaseSchemaHandler : IRequestHandler<ReadDatabaseSchemaCommand, ReadDatabaseSchemaResult>
{
    private readonly ISchemaReaderFactory _schemaReaderFactory;
    private readonly ILogger<ReadDatabaseSchemaHandler> _logger;

    public ReadDatabaseSchemaHandler(
        ISchemaReaderFactory schemaReaderFactory,
        ILogger<ReadDatabaseSchemaHandler> logger)
    {
        _schemaReaderFactory = schemaReaderFactory;
        _logger = logger;
    }

    public async Task<ReadDatabaseSchemaResult> Handle(
        ReadDatabaseSchemaCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var reader = _schemaReaderFactory.Create(request.Provider);

            var connectionValid = await reader.TestConnectionAsync(request.ConnectionString, cancellationToken);

            if (request.TestOnly)
            {
                return new ReadDatabaseSchemaResult
                {
                    Success = connectionValid,
                    ConnectionValid = connectionValid,
                    ErrorMessage = connectionValid ? null : "Connection test failed."
                };
            }

            if (!connectionValid)
            {
                return new ReadDatabaseSchemaResult
                {
                    Success = false,
                    ConnectionValid = false,
                    ErrorMessage = "Cannot read schema: database connection failed."
                };
            }

            var schema = await reader.ReadSchemaAsync(request.ConnectionString, cancellationToken);

            var summary = $"Database: {schema.DatabaseName}\n" +
                          $"Provider: {schema.Provider}\n" +
                          $"Tables/Collections: {schema.Tables.Count}\n" +
                          $"Relationships: {schema.Relationships.Count}";

            _logger.LogInformation("Schema read successfully from {Database}, {TableCount} tables",
                schema.DatabaseName, schema.Tables.Count);

            return new ReadDatabaseSchemaResult
            {
                Success = true,
                ConnectionValid = true,
                Schema = schema,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read database schema");
            return new ReadDatabaseSchemaResult
            {
                Success = false,
                ConnectionValid = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
