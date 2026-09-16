using System.Text;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (lineNumber == 1 &&
                line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add($"рядок {lineNumber}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(
            Separator,
            StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 5 } =>
                new ParseFailed(
                    $"очікую 5 колонок, отримав {parts.Length}"),

            ["", _, _, _, _] or
            [_, "", _, _, _] or
            [_, _, "", _, _] or
            [_, _, _, "", _] =>
                new ParseFailed(
                    "ідентифікатор, SKU, назва або одиниця вимірювання порожні"),

            [_, _, _, _, var quantity]
                when !int.TryParse(quantity, out int parsedQuantity)
                     || parsedQuantity < 0 =>
                new ParseFailed(
                    $"кількість '{quantity}' не є невід'ємним цілим числом"),

            [var id, var sku, var name, var unit, var quantity] =>
                new ParseOk(
                    new ProductDto(
                        id,
                        sku,
                        name,
                        unit,
                        int.Parse(quantity))),

            _ =>
                new ParseFailed(
                    $"занадто багато колонок: {parts.Length}")
        };
    }

    private abstract record ParseOutcome;

    private sealed record ParseOk(ProductDto Value) : ParseOutcome;

    private sealed record ParseFailed(string Reason) : ParseOutcome;
}