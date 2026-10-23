using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Reports;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class CatalogTests
{
    private static Product[] Items() => [Product.Create("a", "SKU-A", "A", "шт", 20), Product.Create("b", "SKU-B", "B", "шт", 20), Product.Create("c", "SKU-C", "C", "шт", 5)];

    [Fact(DisplayName = "Прихід: від’ємна кількість не змінює залишок і не створює подію")]
    public void Receive_Negative_KeepsStateAndDoesNotNotify()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var p = service.Add("SKU", "Товар", "шт", 10);
        int events = 0;
        service.Changed += (_, _) => events++;
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Receive(p.Id, -5));
        Assert.Equal(10, p.Quantity);
        Assert.Equal(0, events);
    }

    [Fact(DisplayName = "Каталог: додавання товару створює рівно одну подію з правильними даними")]
    public void Add_NotifiesExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var p = service.Add("SKU", "Товар", "шт", 7);
        var change = Assert.Single(events);
        Assert.Equal(p.Id, change.Id);
        Assert.Equal(CatalogChangeKind.Added, change.Kind);
        Assert.Equal(7, change.Delta);
        Assert.Equal(7, change.QuantityAfter);
    }

    [Fact(DisplayName = "Видача: перевищення залишку спричиняє DomainException без зміни кількості")]
    public void Issue_AboveStock_ThrowsDomainException()
    {
        var p = Items()[0];
        Assert.Throws<DomainException>(() => p.Issue(21));
        Assert.Equal(20, p.Quantity);
    }

    [Theory(DisplayName = "Звіт: малі залишки містять лише товари з кількістю строго нижче порога")]
    [InlineData(10, 1)]
    [InlineData(5, 0)]
    public void LowStock_IsStrictlyBelowThreshold(int threshold, int count) =>
        Assert.Equal(count, ReportService.LowStock(Items(), threshold).Count);

    [Fact(DisplayName = "Звіт: топ за однакових залишків упорядковується за SKU незалежно від порядку джерела")]
    public void Top_IsDeterministicWithTies()
    {
        string[] expected = ["SKU-A", "SKU-B"];
        Assert.Equal(expected, ReportService.TopByQuantity(Items(), 2).Select(p => p.Sku));
        Assert.Equal(expected, ReportService.TopByQuantity(Items().Reverse(), 2).Select(p => p.Sku));
    }

    [Fact(DisplayName = "Імпорт: скасований токен зупиняє операцію до відкриття файла")]
    public async Task CancelledImport_DoesNotOpenMissingFile()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var service = new CatalogService(new InMemoryCatalogStore());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ImportAsync(new JsonCatalogImporter(), "missing.json", cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new JsonCatalogImporter().LoadAsync("missing.json", cts.Token));
        Assert.Empty(service.All());
    }

    [Fact(DisplayName = "Імпорт: повторний запуск пропускає наявні товари й передає токен імпортеру")]
    public async Task RepeatedImport_SkipsExistingAndPassesToken()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var importer = new StubImporter();
        using var cts = new CancellationTokenSource();
        int events = 0;
        service.Changed += (_, _) => events++;
        Assert.Equal(3, await service.ImportAsync(importer, "unused", cts.Token));
        Assert.Equal(cts.Token, importer.Token);
        Assert.Equal(0, await service.ImportAsync(importer, "unused", cts.Token));
        Assert.Equal(3, events);
        Assert.Equal(3, service.All().Count);
    }

    [Fact(DisplayName = "Імпорт: скасування після першого товару зупиняє додавання наступного")]
    public async Task CancellationInLoop_StopsBeforeSecondItem()
    {
        using var cts = new CancellationTokenSource();
        var service = new CatalogService(new InMemoryCatalogStore());
        service.Changed += (_, _) => cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ImportAsync(new StubImporter(), "unused", cts.Token));
        Assert.Single(service.All());
    }

    [Fact(DisplayName = "JSON: імпортер читає товари, перевіряє дані та зберігає типи технічних помилок")]
    public async Task JsonImporter_ValidatesDataAndPreservesTechnicalErrors()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lab08-{Guid.NewGuid():N}.json");
        const string good = """{"id":"a","sku":"SKU","name":"Товар","unit":"шт","quantity":2}""";
        var importer = new JsonCatalogImporter();
        try
        {
            await File.WriteAllTextAsync(path, $"[{good}]");
            Assert.Equal(2, Assert.Single(await importer.LoadAsync(path)).Quantity);
            await File.WriteAllTextAsync(path, $"[{good},{good.Replace(":2", ":-1")}]");
            var service = new CatalogService(new InMemoryCatalogStore());
            var ex = await Assert.ThrowsAsync<DomainException>(() => service.ImportAsync(importer, path));
            Assert.Contains("Запис 2", ex.Message);
            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
            Assert.Empty(service.All());
            await File.WriteAllTextAsync(path, "[{}]");
            Assert.Contains("поле 'id'", (await Assert.ThrowsAsync<DomainException>(() => importer.LoadAsync(path))).Message);
            await File.WriteAllTextAsync(path, "null");
            await Assert.ThrowsAsync<DomainException>(() => importer.LoadAsync(path));
            await File.WriteAllTextAsync(path, "[");
            await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => importer.LoadAsync(path));
        }
        finally { File.Delete(path); }
        await Assert.ThrowsAsync<FileNotFoundException>(() => importer.LoadAsync(path));
    }

    private sealed class StubImporter : ICatalogImporter
    {
        public CancellationToken Token { get; private set; }
        public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
        {
            Token = ct;
            return Task.FromResult<IReadOnlyList<Product>>(Items());
        }
    }
}
