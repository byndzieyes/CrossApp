using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new DomainException("Кореневий JSON-елемент має бути списком товарів.");

        var result = new List<Product>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            int number = result.Count + 1;
            if (element.ValueKind != JsonValueKind.Object)
                throw new DomainException($"Запис {number}: очікується об'єкт товару.");
            foreach (string field in new[] { "id", "sku", "name", "unit", "quantity" })
            {
                if (!element.EnumerateObject().Any(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase)))
                    throw new DomainException($"Запис {number}: відсутнє поле '{field}'.");
            }
            var dto = element.Deserialize<ProductDto>(Options)!;
            try { result.Add(Product.FromDto(dto)); }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {number}: {ex.Message}", ex);
            }
        }
        return result.AsReadOnly();
    }
}
