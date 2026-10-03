using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class CatalogService(ICatalogStore store)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Product Add(string sku, string name, string unit, int quantity, string? note = null)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N"), sku, name, unit, quantity, note);
        _store.Add(product);
        return product;
    }

    public void Receive(string id, int quantity)
    {
        Product product = RequireProduct(id);
        product.RegisterArrival(quantity);
        _store.Update(product);
    }

    public void Issue(string id, int quantity)
    {
        Product product = RequireProduct(id);
        product.Issue(quantity);
        _store.Update(product);
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Product> Search(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList().AsReadOnly();
    }

    private Product RequireProduct(string id) => _store.GetById(id)
        ?? throw new InvalidOperationException($"Немає товару з id={id}.");
}
