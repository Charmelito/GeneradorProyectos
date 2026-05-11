using Generador.CharmelCodeIA.Domain.Enums;

namespace Generador.CharmelCodeIA.Domain.Interfaces;

public interface ISchemaReaderFactory
{
    IDatabaseSchemaReader Create(DatabaseProviderType providerType);
}
