using Core.Dto;
using Core.Import;

string path = args.Length > 0
    ? args[0]
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();

if (extension is not ".csv" and not ".json")
{
    Console.WriteLine($"Непідтримуваний формат файлу: {extension}");
    return 1;
}

if (path.EndsWith(".mixed.csv", StringComparison.OrdinalIgnoreCase))
{
    return RunMixedImport(path);
}

ImportResult<ProductDto> result = extension switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => throw new InvalidOperationException()
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");

foreach (ProductDto product in result.Items.Take(5))
{
    Console.WriteLine(
        $"  {product.Id,-6} " +
        $"{product.Sku,-10} " +
        $"{product.Name,-35} " +
        $"{product.Quantity,6} " +
        $"{product.Unit}");
}

if (result.Errors.Count > 0)
{
    string skippedLabel = extension == ".json"
        ? "Пропущено елементів"
        : "Пропущено рядків";

    Console.WriteLine();
    Console.WriteLine($"{skippedLabel}: {result.Errors.Count}");

    foreach (string error in result.Errors)
    {
        Console.WriteLine($"  ! {error}");
    }
}

return 0;

static int RunMixedImport(string path)
{
    ImportResult<WarehouseEntryDto> result =
        MixedWarehouseCsvImporter.Load(path);

    Console.WriteLine($"Завантажено записів: {result.Items.Count}");

    foreach (WarehouseEntryDto item in result.Items)
    {
        string output = item switch
        {
            ProductDto product =>
                $"  [товар] {product.Id,-6} {product.Sku,-10} " +
                $"{product.Name,-30} {product.Quantity,6} {product.Unit}",

            WarehouseDto warehouse =>
                $"  [склад] {warehouse.Id,-6} " +
                $"{warehouse.Name,-25} {warehouse.Location}",

            _ => "  [невідомий запис]"
        };

        Console.WriteLine(output);
    }

    if (result.Errors.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");

        foreach (string error in result.Errors)
        {
            Console.WriteLine($"  ! {error}");
        }
    }

    return 0;
}
