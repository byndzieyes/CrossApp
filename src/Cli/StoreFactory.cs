using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

public static class StoreFactory
{
    public static string DataPath => Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

    public static bool IsCatalogMode(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.All(arg => arg is "--file" or "--cache")
            && args.Distinct().Count() == args.Length;
    }

    public static ICatalogStore Create(string[] args)
    {
        if (!IsCatalogMode(args))
            throw new ArgumentException("Для каталогу доступні лише [--file] [--cache] без повторень.", nameof(args));

        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(DataPath)
            : new InMemoryCatalogStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}
