namespace Core.Domain;

// Додатна Delta означає прихід, від'ємна — видачу.
public sealed record StockMovement(string ProductId, int Delta);
