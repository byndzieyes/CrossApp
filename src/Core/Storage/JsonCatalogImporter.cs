using System.Text.Json;
using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        var result = new List<Product>();
        await foreach (var product in StreamAsync(path, ct))
            result.Add(product);
        return result.AsReadOnly();
    }

    public async IAsyncEnumerable<Product> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
        {
            var character = new char[1];
            while (true)
            {
                int read = await reader.ReadAsync(character.AsMemory(), ct);
                if (read == 0)
                    throw new JsonException("Порожній JSON-документ.");
                if (character[0] is ' ' or '\t' or '\r' or '\n') continue;
                if (character[0] != '[')
                    throw new DomainException("Кореневий JSON-елемент має бути списком товарів.");
                break;
            }
        }
        stream.Position = 0;
        int number = 0;
        int lastPercent = 0;
        await foreach (var element in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
            stream, Options, cancellationToken: ct))
        {
            ct.ThrowIfCancellationRequested();
            number++;
            if (element.ValueKind != JsonValueKind.Object)
                throw new DomainException($"Запис {number}: очікується об'єкт товару.");
            foreach (string field in new[] { "id", "sku", "name", "unit", "quantity" })
            {
                if (!element.EnumerateObject().Any(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase)))
                    throw new DomainException($"Запис {number}: відсутнє поле '{field}'.");
            }
            var dto = element.Deserialize<ProductDto>(Options)!;
            Product product;
            try { product = Product.FromDto(dto); }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {number}: {ex.Message}", ex);
            }
            yield return product;
            ct.ThrowIfCancellationRequested();
            int percent = (int)Math.Min(99, stream.Position * 100d / stream.Length);
            if (percent > lastPercent)
            {
                progress?.Report(percent);
                lastPercent = percent;
            }
        }
    }
}
