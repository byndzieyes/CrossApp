namespace Core.Extensions;

public static class EnumerableExtensions
{
    // Другий ключ визначає порядок елементів з однаковим показником.
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source,
        Func<T, int> selector,
        int n,
        Func<T, string> tieBreaker)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(tieBreaker);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return source.OrderByDescending(selector)
            .ThenBy(tieBreaker, StringComparer.Ordinal)
            .Take(n);
    }
}
