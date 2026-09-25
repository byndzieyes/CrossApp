namespace Core.Domain;

public sealed class Warehouse
{
    private int _occupiedCapacity;

    public string Id { get; }
    public string Name { get; }
    public string Unit { get; }
    public int Capacity { get; }
    public int OccupiedCapacity => _occupiedCapacity;
    public int FreeCapacity => Capacity - _occupiedCapacity;

    private Warehouse(string id, string name, string unit, int capacity, int occupiedCapacity)
    {
        Id = id;
        Name = name;
        Unit = unit;
        Capacity = capacity;
        _occupiedCapacity = occupiedCapacity;
    }

    public static Warehouse Create(
        string id, string name, string unit, int capacity, int occupiedCapacity = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця місткості складу не може бути порожньою", nameof(unit));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                "Місткість складу має бути більшою за нуль");

        if (occupiedCapacity < 0 || occupiedCapacity > capacity)
            throw new ArgumentOutOfRangeException(nameof(occupiedCapacity), occupiedCapacity,
                "Зайняте місце має бути в межах місткості складу");

        return new Warehouse(id.Trim(), name.Trim(), unit.Trim(), capacity, occupiedCapacity);
    }

    internal void Store(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість надходження має бути більшою за нуль");

        if (amount > FreeCapacity)
            throw new InvalidOperationException(
                $"На складі {Name} вільно лише {FreeCapacity} {Unit}");

        _occupiedCapacity += amount;
    }
}
