using Core.Dto;

namespace Core.Domain;

public sealed class Product
{
    private int _quantity;

    public string Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Unit { get; }
    public int Quantity => _quantity;
    public string? Note { get; }

    private Product(
        string id,
        string sku,
        string name,
        string unit,
        int quantity,
        string? note)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Unit = unit;
        _quantity = quantity;
        Note = note;
    }

    public static Product Create(
        string id,
        string sku,
        string name,
        string unit,
        int quantity,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор товару не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU товару не може бути порожнім", nameof(sku));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва товару не може бути порожньою", nameof(name));

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця вимірювання не може бути порожньою", nameof(unit));

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity), quantity, "Початковий залишок не може бути від'ємним");

        return new Product(
            id.Trim(),
            sku.Trim().ToUpperInvariant(),
            name.Trim(),
            unit.Trim(),
            quantity,
            note);
    }

    public void RegisterArrival(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount), amount, "Кількість приходу має бути більшою за нуль");

        if (amount > int.MaxValue - _quantity)
            throw new InvalidOperationException(
                $"Не можна додати {amount} одиниць товару {Sku}: залишок перевищить допустиме значення");

        _quantity += amount;
    }

    public void Issue(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount), amount, "Кількість видачі має бути більшою за нуль");

        if (amount > _quantity)
            throw new InvalidOperationException(
                $"Не можна видати {amount} одиниць товару {Sku}: залишок становить {_quantity}");

        _quantity -= amount;
    }

    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Quantity, Note);

    public static Product FromDto(ProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Quantity, dto.Note);
    }
}
