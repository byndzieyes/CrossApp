using System.Diagnostics;
using Core.Domain;

namespace Core.Reports;

public sealed record BenchmarkSample(double WithoutIntermediateListMs, double WithIntermediateListMs);

public sealed record ReportBenchmarkResult(
    int SourceCount,
    int ResultCount,
    IReadOnlyList<BenchmarkSample> Samples,
    double WithoutIntermediateListMedianMs,
    double WithIntermediateListMedianMs);

public static class ReportBenchmark
{
    public static ReportBenchmarkResult Run(int count = 100_000, int repetitions = 9)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(repetitions, 1);

        var random = new Random(7);
        var source = Enumerable.Range(1, count)
            .Select(i => Product.Create($"B-{i:D6}", $"SKU-{i:D6}",
                $"Товар {i}", "шт", random.Next(1000)))
            .ToList();
        var expected = BuildReport(source, false);

        // Прогрів обох варіантів перед вимірюванням.
        for (int i = 0; i < 3; i++)
        {
            Verify(BuildReport(source, false), expected);
            Verify(BuildReport(source, true), expected);
        }

        double Measure(bool intermediateList)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = BuildReport(source, intermediateList);
            stopwatch.Stop();
            Verify(result, expected);
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        var samples = new List<BenchmarkSample>();
        for (int i = 0; i < repetitions; i++)
        {
            double without, with;
            if (i % 2 == 0)
            {
                without = Measure(false);
                with = Measure(true);
            }
            else
            {
                with = Measure(true);
                without = Measure(false);
            }
            samples.Add(new BenchmarkSample(without, with));
        }

        return new ReportBenchmarkResult(source.Count, expected.Count, samples.AsReadOnly(),
            Median(samples.Select(s => s.WithoutIntermediateListMs)),
            Median(samples.Select(s => s.WithIntermediateListMs)));
    }

    private static List<StockLine> BuildReport(IEnumerable<Product> source, bool intermediateList)
    {
        var filtered = source.Where(p => p.Quantity < 500);
        if (intermediateList)
            filtered = filtered.ToList();

        return filtered.OrderBy(p => p.Quantity)
            .ThenBy(p => p.Sku, StringComparer.Ordinal)
            .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
            .ToList();
    }

    private static void Verify(IReadOnlyList<StockLine> actual, IReadOnlyList<StockLine> expected)
    {
        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException("Варіанти звіту повернули різні результати.");
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        int middle = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[middle - 1] + sorted[middle]) / 2
            : sorted[middle];
    }
}
