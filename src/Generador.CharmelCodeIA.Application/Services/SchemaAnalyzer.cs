using Generador.CharmelCodeIA.Domain.Entities;

namespace Generador.CharmelCodeIA.Application.Services;

public sealed class SchemaAnalyzer
{
    public IReadOnlyList<ValueObjectCandidate> DetectValueObjects(DatabaseSchema schema)
    {
        var candidates = new List<ValueObjectCandidate>();

        foreach (var table in schema.Tables)
        {
            candidates.AddRange(DetectInTable(table));
        }

        return candidates;
    }

    private static IEnumerable<ValueObjectCandidate> DetectInTable(TableDefinition table)
    {
        var columns = table.Columns.ToList();

        // Detect Email
        var emailColumn = columns.FirstOrDefault(c =>
            c.Name.Contains("Email", StringComparison.OrdinalIgnoreCase) &&
            c.ClrType == "string" &&
            c.MaxLength is > 10 and <= 320);
        if (emailColumn != null)
        {
            yield return new ValueObjectCandidate
            {
                TableName = table.Name,
                Name = "Email",
                SourceColumns = new[] { emailColumn.Name },
                Reason = "Email pattern detected"
            };
        }

        // Detect Money (Amount + Currency pair)
        var amountColumn = columns.FirstOrDefault(c =>
            c.Name.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("Total", StringComparison.OrdinalIgnoreCase));
        if (amountColumn != null && amountColumn.ClrType is "decimal" or "decimal?")
        {
            var currencyColumn = columns.FirstOrDefault(c =>
                c.Name.Contains("Currency", StringComparison.OrdinalIgnoreCase));
            yield return new ValueObjectCandidate
            {
                TableName = table.Name,
                Name = "Money",
                SourceColumns = currencyColumn != null
                    ? new[] { amountColumn.Name, currencyColumn.Name }
                    : new[] { amountColumn.Name },
                Reason = "Money pattern detected"
            };
        }

        // Detect Address (multiple address-like columns)
        var addressColumns = columns.Where(c =>
            c.Name is "Street" or "City" or "State" or "ZipCode" or "PostalCode" or "Country" or "AddressLine1" or "AddressLine2")
            .ToList();
        if (addressColumns.Count >= 3)
        {
            yield return new ValueObjectCandidate
            {
                TableName = table.Name,
                Name = "Address",
                SourceColumns = addressColumns.Select(c => c.Name).ToList(),
                Reason = "Address pattern detected (multiple address columns)"
            };
        }

        // Detect Phone
        var phoneColumns = columns.Where(c =>
            c.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase) &&
            c.ClrType == "string").ToList();
        foreach (var phone in phoneColumns)
        {
            yield return new ValueObjectCandidate
            {
                TableName = table.Name,
                Name = "PhoneNumber",
                SourceColumns = new[] { phone.Name },
                Reason = "Phone number pattern detected"
            };
        }

        // Detect Percentage
        var percentageColumns = columns.Where(c =>
            c.Name.Contains("Percent", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) && c.ClrType is "decimal" or "decimal?").ToList();
        foreach (var pct in percentageColumns)
        {
            yield return new ValueObjectCandidate
            {
                TableName = table.Name,
                Name = "Percentage",
                SourceColumns = new[] { pct.Name },
                Reason = "Percentage pattern detected"
            };
        }

        // Detect Coordinate pair
        var lat = columns.FirstOrDefault(c =>
            c.Name.Contains("Latitude", StringComparison.OrdinalIgnoreCase) ||
            c.Name is "Lat" or "Latitude");
        var lng = columns.FirstOrDefault(c =>
            c.Name.Contains("Longitude", StringComparison.OrdinalIgnoreCase) ||
            c.Name is "Lng" or "Lon" or "Long" or "Longitude");
        if (lat != null && lng != null)
        {
            yield return new ValueObjectCandidate
            {
                TableName = table.Name,
                Name = "GeoCoordinate",
                SourceColumns = new[] { lat.Name, lng.Name },
                Reason = "Coordinate pattern detected"
            };
        }
    }
}

public sealed class ValueObjectCandidate
{
    public string TableName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> SourceColumns { get; init; } = Array.Empty<string>();
    public string Reason { get; init; } = string.Empty;
}
