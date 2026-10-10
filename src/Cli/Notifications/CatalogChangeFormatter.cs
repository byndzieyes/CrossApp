using Core.Events;

namespace Cli.Notifications;

internal static class CatalogChangeFormatter
{
    public static string Format(CatalogChangedEventArgs eventArgs) =>
        $"{eventArgs.At:HH:mm:ss} {eventArgs.Kind,-8} {eventArgs.Id} " +
        $"{eventArgs.Delta,4:+0;-0} → залишок {eventArgs.QuantityAfter}";
}
