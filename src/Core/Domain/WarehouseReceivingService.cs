namespace Core.Domain;

public sealed class WarehouseReceivingService
{
    public void Receive(Warehouse warehouse, Product product, int amount)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(product);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість надходження має бути більшою за нуль");

        if (!string.Equals(warehouse.Unit, product.Unit, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Склад {warehouse.Name} приймає {warehouse.Unit}, а товар {product.Sku} має одиницю {product.Unit}");

        if (amount > warehouse.FreeCapacity)
            throw new InvalidOperationException(
                $"На складі {warehouse.Name} вільно лише {warehouse.FreeCapacity} {warehouse.Unit}");

        if (amount > int.MaxValue - product.Quantity)
            throw new InvalidOperationException(
                $"Надходження переповнить залишок товару {product.Sku}");

        product.RegisterArrival(amount);
        warehouse.Store(amount);
    }
}
