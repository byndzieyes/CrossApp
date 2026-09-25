using Core.Dto;
using Core.Import;
using Core.Domain;

if (args.Length == 1 &&
    args[0].Equals("--demo-domain", StringComparison.OrdinalIgnoreCase))
{
    RunDomainDemo();
    return 0;
}

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

PrintStatistics(result);

return 0;

static void RunDomainDemo()
{
    Console.WriteLine("=== Сценарій 1: успішні операції ===");

    Product product = Product.Create(
        "P-001", "sku-001", "Цемент М400 25кг", "шт", 100, "Демонстраційний товар");

    Console.WriteLine($"Початковий залишок {product.Sku}: {product.Quantity} {product.Unit}");
    product.RegisterArrival(50);
    product.Issue(30);
    Console.WriteLine($"Після приходу 50 і видачі 30: {product.Quantity} {product.Unit}");

    ProductDto dto = product.ToDto();
    Product restored = Product.FromDto(dto);
    Console.WriteLine(
        $"Product → DTO → Product: {restored.Sku}, " +
        $"залишок {restored.Quantity} {restored.Unit}, примітка: {restored.Note}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

    int quantityBeforeFailures = product.Quantity;
    TryDo("видача більша за залишок", () => product.Issue(1000));
    TryDo("нульова кількість приходу", () => product.RegisterArrival(0));
    TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
    TryDo("від'ємний початковий залишок", () =>
        Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
    TryDo("некоректний DTO", () => Product.FromDto(
        new ProductDto("P-004", "SKU-004", "Щебінь", "т", -1)));

    Console.WriteLine(
        $"Залишок після відмов: {product.Quantity} {product.Unit} " +
        $"(до них: {quantityBeforeFailures} {product.Unit})");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 3: DTO → доменні товари ===");

    var imported = new ImportResult<ProductDto>(
        [
            new ProductDto("P-010", "sku-010", "Пісок", "т", 15),
            new ProductDto("P-011", "sku-011", "Цегла", "шт", -3),
            new ProductDto("P-012", "sku-012", "Щебінь", "т", 8)
        ],
        ["рядок 7: кількість 'багато' не є цілим числом"]);

    ImportResult<Product> converted = ProductDomainConverter.Convert(imported);
    Console.WriteLine($"DTO на вході: {imported.Items.Count}");
    Console.WriteLine($"Доменних товарів: {converted.Items.Count}");

    foreach (Product accepted in converted.Items)
    {
        Console.WriteLine($"  + {accepted.Id}: {accepted.Sku}, {accepted.Quantity} {accepted.Unit}");
    }

    Console.WriteLine($"Помилок (імпорт + домен): {converted.Errors.Count}");

    foreach (string error in converted.Errors)
    {
        Console.WriteLine($"  ! {error}");
    }
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"{title}: виняток не спрацював — інваріант відсутній");
    }
    catch (Exception exception)
    {
        string message = exception.Message.Replace(Environment.NewLine, " ");
        Console.WriteLine(
            $"{title}: {exception.GetType().Name} — {message}");
    }
}

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

    PrintStatistics(result);

    return 0;
}

static void PrintStatistics<T>(ImportResult<T> result)
{
    int accepted = result.Items.Count;
    int skipped = result.Errors.Count;
    int total = accepted + skipped;
    double errorPercentage = total == 0
        ? 0
        : skipped * 100.0 / total;

    Console.WriteLine();
    Console.WriteLine(
        $"Статистика: усього {total}, " +
        $"прийнято {accepted}, " +
        $"пропущено {skipped}, " +
        $"помилок {errorPercentage:F1}%");
}
