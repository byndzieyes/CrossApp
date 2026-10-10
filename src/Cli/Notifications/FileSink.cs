using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = string.IsNullOrWhiteSpace(path)
        ? throw new ArgumentException("Шлях до файлу журналу не може бути порожнім.", nameof(path))
        : path;

    public void Notify(CatalogChangedEventArgs eventArgs)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string line = CatalogChangeFormatter.Format(eventArgs);
            File.AppendAllText(_path, line + Environment.NewLine);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Не вдалося записати подію у файл: {exception.Message}");
        }
    }
}
