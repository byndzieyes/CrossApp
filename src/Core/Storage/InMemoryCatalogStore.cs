using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryCatalogStore(IEnumerable<Product>? seed = null) : ICatalogStore
{
    private readonly Dictionary<string, Product> _items =
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Product> List() => _items.Values.ToList().AsReadOnly();

    public Product? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.TryAdd(item.Id, item))
            throw new InvalidOperationException($"Товар з id={item.Id} уже існує.");
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає товару з id={item.Id} для оновлення.");
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);
}
