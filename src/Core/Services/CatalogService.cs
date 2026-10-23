using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class CatalogService(
    ICatalogStore store,
    IEnumerable<INotificationSink>? notificationSinks = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _notificationSinks = notificationSinks?.ToArray() ?? [];

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(Product product, CatalogChangeKind kind, int delta)
    {
        var eventArgs = new CatalogChangedEventArgs(
            product.Id, kind, delta, product.Quantity);

        Changed?.Invoke(this, eventArgs);

        foreach (INotificationSink sink in _notificationSinks)
            sink.Notify(eventArgs);
    }

    public Product Add(string sku, string name, string unit, int quantity, string? note = null)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N"), sku, name, unit, quantity, note);
        _store.Add(product);
        OnChanged(product, CatalogChangeKind.Added, quantity);
        return product;
    }

    public void Receive(string id, int quantity)
    {
        Product product = RequireProduct(id);
        product.RegisterArrival(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        Product product = RequireProduct(id);
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public IReadOnlyList<Product> All() => _store.List();

    public async Task<int> ImportAsync(ICatalogImporter importer, string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(importer);
        var items = await importer.LoadAsync(path, ct);
        ct.ThrowIfCancellationRequested();
        int added = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(item.Id) is not null) continue;
            _store.Add(item);
            OnChanged(item, CatalogChangeKind.Added, item.Quantity);
            added++;
        }
        return added;
    }

    public Product? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Product> Search(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList().AsReadOnly();
    }

    private Product RequireProduct(string id) => _store.GetById(id)
        ?? throw new DomainException($"Немає товару з id={id}.");
}
