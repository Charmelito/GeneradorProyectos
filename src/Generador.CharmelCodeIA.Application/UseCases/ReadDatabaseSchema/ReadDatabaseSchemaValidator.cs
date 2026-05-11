using FluentValidation;

namespace Generador.CharmelCodeIA.Application.UseCases.ReadDatabaseSchema;

public sealed class ReadDatabaseSchemaValidator : AbstractValidator<ReadDatabaseSchemaCommand>
{
    public ReadDatabaseSchemaValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .WithMessage("Connection string is required.");

        RuleFor(x => x.ConnectionString)
            .Must(cs => cs.Contains("Server") || cs.Contains("Host") || cs.Contains("mongodb://") || cs.Contains("AccountEndpoint"))
            .WithMessage("Connection string appears invalid.");
    }
}
