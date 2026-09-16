using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new ImportResult<ProductDto>(
                    items,
                    ["кореневий JSON-елемент має бути масивом"]);
            }

            int elementNumber = 0;

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                elementNumber++;

                if (element.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"елемент {elementNumber}: очікується JSON-об'єкт");
                    continue;
                }

                try
                {
                    ProductDto? product = element.Deserialize<ProductDto>(Options);

                    if (product is null)
                    {
                        errors.Add($"елемент {elementNumber}: не вдалося створити товар");
                        continue;
                    }

                    string? validationError = Validate(product);

                    if (validationError is not null)
                    {
                        errors.Add($"елемент {elementNumber}: {validationError}");
                        continue;
                    }

                    items.Add(product);
                }
                catch (JsonException)
                {
                    errors.Add(
                        $"елемент {elementNumber}: невідповідний тип або структура даних");
                }
            }

            return new ImportResult<ProductDto>(items, errors);
        }
        catch (JsonException exception)
        {
            return new ImportResult<ProductDto>(
                items,
                [$"некоректний JSON: {exception.Message}"]);
        }
    }

    private static string? Validate(ProductDto product) => product switch
    {
        { Id: var id } when string.IsNullOrWhiteSpace(id) =>
            "ідентифікатор порожній",
        { Sku: var sku } when string.IsNullOrWhiteSpace(sku) =>
            "SKU порожній",
        { Name: var name } when string.IsNullOrWhiteSpace(name) =>
            "назва порожня",
        { Unit: var unit } when string.IsNullOrWhiteSpace(unit) =>
            "одиниця вимірювання порожня",
        { Quantity: < 0 } => "кількість не може бути від'ємною",
        _ => null
    };
}
