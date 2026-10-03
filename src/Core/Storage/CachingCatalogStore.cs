using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Product?> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Product>? _list;

    public IReadOnlyList<Product> List()
    {
        if (_list is not null) return _list;
        _list = _inner.List().ToList().AsReadOnly();
        foreach (Product product in _list)
            _byId[product.Id] = product;
        return _list;
    }

    public Product? GetById(string id)
    {
        if (_byId.TryGetValue(id, out Product? product)) return product;
        product = _inner.GetById(id);
        _byId[id] = product; // Відсутність товару також кешується.
        return product;
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Invalidate();
        _inner.Add(item);
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Invalidate();
        _inner.Update(item);
    }

    public bool Remove(string id)
    {
        Invalidate();
        return _inner.Remove(id);
    }

    private void Invalidate()
    {
        // До запису: внутрішнє сховище може змінитися навіть при помилці запису.
        _list = null;
        _byId.Clear();
    }
}
