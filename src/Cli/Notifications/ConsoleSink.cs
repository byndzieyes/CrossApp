using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs eventArgs) =>
        Console.WriteLine(CatalogChangeFormatter.Format(eventArgs));
}
