using Core.Domain;

namespace Core.Reports;

public static class DeferredExecutionDemo
{
    public static (int Before, int After, int Snapshot, int Remaining) Run(IEnumerable<Product> items)
    {
        var source = items.ToList();
        var query = source.Where(p => p.Quantity < 10);
        int before = query.Count();
        source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
        int after = query.Count();
        var snapshot = query.ToList();
        source.Clear();
        return (before, after, snapshot.Count, source.Count);
    }
}
