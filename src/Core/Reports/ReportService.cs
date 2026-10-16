using Core.Domain;
using Core.Extensions;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<MovementSummary> ByMovements(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        products.GroupJoin(movements,
                product => product.Id,
                movement => movement.ProductId,
                (product, matches) => new MovementSummary(
                    product.Sku, product.Name, product.Unit,
                    matches.Count(),
                    matches.Where(m => m.Delta > 0).Sum(m => m.Delta),
                    matches.Where(m => m.Delta < 0).Sum(m => -m.Delta),
                    matches.Sum(m => m.Delta)),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(line => line.Sku, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
            .OrderBy(p => p.Quantity)
            .ThenBy(p => p.Sku, StringComparer.Ordinal)
            .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
            .ToList();

    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, n, p => p.Sku)
            .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
            .ToList();

    public static IReadOnlyList<MovementSummary> TopByMovementCount(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements, int n) =>
        ByMovements(products, movements)
            .TopBy(line => line.MovementCount, n, line => line.Sku)
            .ToList();

    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source)
    {
        var query =
            from g in source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
            select new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)) into summary
            orderby summary.TotalQuantity descending
            select summary;

        return query
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity),
                list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }
}
