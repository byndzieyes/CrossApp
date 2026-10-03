using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileCatalogStore : ICatalogStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path;
    private bool _loaded;

    public FileCatalogStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;

        // Спочатку перевіряємо весь файл, щоб помилка не залишила частковий кеш.
        var loaded = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(_path))
        {
            var dtos = JsonSerializer.Deserialize<List<ProductDto>>(File.ReadAllText(_path)) ?? [];
            foreach (ProductDto dto in dtos)
            {
                Product product = Product.FromDto(dto);
                if (!loaded.TryAdd(product.Id, product))
                    throw new InvalidOperationException($"У файлі повторюється id={product.Id}.");
            }
        }

        foreach (var entry in loaded)
            _cache.Add(entry.Key, entry.Value);
        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var dtos = _cache.Values.Select(product => product.ToDto()).ToList();
        File.WriteAllText(_path, JsonSerializer.Serialize(dtos, Options));
    }

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList().AsReadOnly();
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.TryAdd(item.Id, item))
            throw new InvalidOperationException($"Товар з id={item.Id} уже існує.");
        Flush();
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає товару з id={item.Id} для оновлення.");
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}
