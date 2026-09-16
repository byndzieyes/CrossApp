using System.Text;
using Core.Dto;

namespace Core.Import;

public static class MixedWarehouseCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<WarehouseEntryDto> Load(string path)
    {
        var items = new List<WarehouseEntryDto>();
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
                line.StartsWith("type", StringComparison.OrdinalIgnoreCase))
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

        return new ImportResult<WarehouseEntryDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(
            Separator,
            StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", "", _, _, _, _] or
            ["P", _, "", _, _, _] or
            ["P", _, _, "", _, _] or
            ["P", _, _, _, "", _] =>
                new ParseFailed(
                    "ідентифікатор, SKU, назва або одиниця вимірювання товару порожні"),

            ["P", _, _, _, _, var quantity]
                when !int.TryParse(quantity, out int parsedQuantity)
                     || parsedQuantity < 0 =>
                new ParseFailed(
                    $"кількість '{quantity}' не є невід'ємним цілим числом"),

            ["P", var id, var sku, var name, var unit, var quantity] =>
                new ParseOk(
                    new ProductDto(
                        id,
                        sku,
                        name,
                        unit,
                        int.Parse(quantity))),

            ["W", "", _, _] or
            ["W", _, "", _] or
            ["W", _, _, ""] =>
                new ParseFailed(
                    "ідентифікатор, назва або розташування складу порожні"),

            ["W", var id, var name, var location] =>
                new ParseOk(new WarehouseDto(id, name, location)),

            [var prefix, ..] when prefix is not "P" and not "W" =>
                new ParseFailed($"невідомий префікс '{prefix}'"),

            ["P", ..] =>
                new ParseFailed(
                    $"для товару очікується 6 колонок, отримано {parts.Length}"),

            ["W", ..] =>
                new ParseFailed(
                    $"для складу очікується 4 колонки, отримано {parts.Length}"),

            _ => new ParseFailed("порожній або некоректний рядок")
        };
    }

    private abstract record ParseOutcome;

    private sealed record ParseOk(WarehouseEntryDto Value) : ParseOutcome;

    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
