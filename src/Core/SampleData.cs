using Core.Domain;

namespace Core;

public static class SampleData
{
    // Кожен виклик створює нові сутності: зміни одного сховища не змінюють seed іншого.
    public static IEnumerable<Product> Products() =>
    [
        Product.Create("P-001", "SKU-001", "Цемент М400 25кг", "шт", 120),
        Product.Create("P-002", "SKU-002", "Пісок будівельний", "т", 18),
        Product.Create("P-003", "SKU-003", "Цегла червона", "шт", 1500),
        Product.Create("P-004", "SKU-004", "Щебінь", "т", 25),
        Product.Create("P-005", "SKU-005", "Арматура 12мм", "м", 300),
        Product.Create("P-006", "SKU-006", "Дошка соснова", "шт", 80),
        Product.Create("P-007", "SKU-007", "Гіпсокартон", "шт", 60),
        Product.Create("P-008", "SKU-008", "Саморізи", "шт", 5000),
        Product.Create("P-009", "SKU-009", "Болт М8", "шт", 700),
        Product.Create("P-010", "SKU-010", "Гайка М8", "шт", 700),
        Product.Create("P-011", "SKU-011", "Кабель UTP cat6", "м", 200),
        Product.Create("P-012", "SKU-012", "Труба ПВХ", "м", 150),
        Product.Create("P-013", "SKU-013", "Фарба біла", "л", 45),
        Product.Create("P-014", "SKU-014", "Ґрунтовка", "л", 35),
        Product.Create("P-015", "SKU-015", "Клей плитковий", "шт", 90),
        Product.Create("P-016", "SKU-016", "Плитка керамічна", "м2", 110),
        Product.Create("P-017", "SKU-017", "Мінеральна вата", "шт", 40),
        Product.Create("P-018", "SKU-018", "Рулон плівки", "шт", 0)
    ];
}
