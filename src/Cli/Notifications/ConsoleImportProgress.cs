namespace Cli.Notifications;

public sealed class ConsoleImportProgress : IProgress<int>
{
    private int _lastPercent = -1;

    public void Report(int value)
    {
        if (value <= _lastPercent) return;
        _lastPercent = value;
        Console.WriteLine($"Імпорт: {value}%");
    }
}
