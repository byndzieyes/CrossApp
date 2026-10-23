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
            Assert.Single(service.All());
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

    [Fact(DisplayName = "Потоковий імпорт: сервіс додає товар до запиту наступного елемента")]
    public async Task StreamingImport_ProcessesEachItemBeforeRequestingNext()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var importer = new OrderedImporter(service);
        Assert.Equal(2, await service.ImportAsync(importer, "unused"));
        Assert.Equal(2, service.All().Count);
    }

    [Fact(DisplayName = "Потоковий JSON: перший товар доступний до помилки другого, перебирання звільняє файл")]
    public async Task JsonStream_YieldsBeforeValidationErrorAndDisposesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lab08-stream-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path,
                """[{"id":"a","sku":"A","name":"Товар","unit":"шт","quantity":2},{}]""");
            await using (var iterator = new JsonCatalogImporter().StreamAsync(path).GetAsyncEnumerator())
            {
                Assert.True(await iterator.MoveNextAsync());
                Assert.Equal("a", iterator.Current.Id);
                var error = await Assert.ThrowsAsync<DomainException>(async () => await iterator.MoveNextAsync());
                Assert.Contains("Запис 2", error.Message);
            }
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.CanWrite);
        }
        finally { File.Delete(path); }
    }

    [Theory(DisplayName = "Зв’язані токени: імпорт скасовується лише активованим джерелом")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LinkedCancellation_IdentifiesTimeoutOrUser(bool useTimeout)
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        var importer = new WaitingImporter();
        var service = new CatalogService(new InMemoryCatalogStore());
        Task<int> operation = service.ImportAsync(importer, "unused", linkedCts.Token);
        try
        {
            await importer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (useTimeout) timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(50));
            else userCts.Cancel();

            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => operation.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Equal(linkedCts.Token, error.CancellationToken);
            Assert.True(linkedCts.IsCancellationRequested);
            Assert.Equal(useTimeout, timeoutCts.IsCancellationRequested);
            Assert.Equal(!useTimeout, userCts.IsCancellationRequested);
            Assert.Empty(service.All());
            Assert.True(importer.Disposed);
        }
        finally
        {
            userCts.Cancel();
            try { await operation.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (OperationCanceledException) { }
        }
    }

    [Theory(DisplayName = "Прогрес імпорту: 100% означає лише успішне завершення, значення не зменшуються")]
    [InlineData("valid")]
    [InlineData("empty")]
    [InlineData("invalid")]
    [InlineData("cancelled")]
    public async Task ImportProgress_ReportsCompletionOnlyOnSuccess(string scenario)
    {
        string path = Path.Combine(Path.GetTempPath(), $"lab08-progress-{Guid.NewGuid():N}.json");
        const string good = """{"id":"progress-1","sku":"SKU","name":"Товар","unit":"шт","quantity":2}""";
        var progress = new RecordingProgress();
        using var cts = new CancellationTokenSource();
        var service = new CatalogService(new InMemoryCatalogStore());
        try
        {
            string json = scenario switch
            {
                "empty" => "[]",
                "invalid" => $"[{good},{{}}]",
                _ => $"[{good}]"
            };
            await File.WriteAllTextAsync(path, json);
            if (scenario == "cancelled") service.Changed += (_, _) => cts.Cancel();
            Task<int> operation = service.ImportAsync(new JsonCatalogImporter(), path, cts.Token, progress);
            if (scenario == "invalid")
                await Assert.ThrowsAsync<DomainException>(() => operation);
            else if (scenario == "cancelled")
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            else
                Assert.Equal(scenario == "empty" ? 0 : 1, await operation);

            Assert.Equal(0, progress.Values[0]);
            Assert.All(progress.Values, value => Assert.InRange(value, 0, 100));
            Assert.Equal(progress.Values.OrderBy(value => value), progress.Values);
            if (scenario is "valid" or "empty")
                Assert.Equal(100, progress.Values[^1]);
            else
                Assert.DoesNotContain(100, progress.Values);
            if (scenario == "valid")
            {
                Assert.Contains(progress.Values, value => value > 0 && value < 100);
                var repeated = new RecordingProgress();
                Assert.Equal(0, await service.ImportAsync(new JsonCatalogImporter(), path, progress: repeated));
                Assert.Equal(100, repeated.Values[^1]);
            }
        }
        finally { File.Delete(path); }
    }

    private sealed class RecordingProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }

    private sealed class WaitingImporter : ICatalogImporter
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Disposed { get; private set; }

        public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default) =>
            throw new InvalidOperationException("Використовується потоковий контракт.");

        public async IAsyncEnumerable<Product> StreamAsync(string path,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default, IProgress<int>? progress = null)
        {
            try
            {
                Started.SetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                yield break;
            }
            finally { Disposed = true; }
        }
    }

    private sealed class OrderedImporter(CatalogService service) : ICatalogImporter
    {
        public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default) =>
            throw new InvalidOperationException("Сервіс повинен використовувати потоковий контракт.");

        public async IAsyncEnumerable<Product> StreamAsync(string path,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default, IProgress<int>? progress = null)
        {
            await Task.CompletedTask;
            ct.ThrowIfCancellationRequested();
            yield return Items()[0];
            Assert.NotNull(service.Find("a"));
            yield return Items()[1];
        }
    }

    private sealed class StubImporter : ICatalogImporter
    {
        public CancellationToken Token { get; private set; }
        public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
        {
            Token = ct;
            return Task.FromResult<IReadOnlyList<Product>>(Items());
        }

        public async IAsyncEnumerable<Product> StreamAsync(string path,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default, IProgress<int>? progress = null)
        {
            Token = ct;
            await Task.CompletedTask;
            foreach (var item in Items())
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }
}
