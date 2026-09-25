namespace Core.Domain;

public sealed class Order
{
    public string Id { get; }
    public OrderStatus Status { get; private set; }

    private Order(string id)
    {
        Id = id;
        Status = OrderStatus.Draft;
    }

    public static Order Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення не може бути порожнім", nameof(id));

        return new Order(id.Trim());
    }

    public void ChangeStatus(OrderStatus nextStatus)
    {
        Status = (Status, nextStatus) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => OrderStatus.Confirmed,
            (OrderStatus.Draft, OrderStatus.Cancelled) => OrderStatus.Cancelled,
            _ => throw new InvalidOperationException(
                $"Перехід замовлення {Id} зі стану {Status} до {nextStatus} заборонено")
        };
    }
}
