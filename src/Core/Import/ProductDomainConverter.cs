using Core.Domain;
using Core.Dto;

namespace Core.Import;

public static class ProductDomainConverter
{
    public static ImportResult<Product> Convert(ImportResult<ProductDto> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var products = new List<Product>();
        var errors = new List<string>(source.Errors);

        for (int i = 0; i < source.Items.Count; i++)
        {
            ProductDto dto = source.Items[i];

            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (ArgumentException exception)
            {
                errors.Add(DescribeError(i, dto, exception));
            }
            catch (InvalidOperationException exception)
            {
                errors.Add(DescribeError(i, dto, exception));
            }
        }

        return new ImportResult<Product>(products.AsReadOnly(), errors.AsReadOnly());
    }

    private static string DescribeError(int index, ProductDto? dto, Exception exception)
    {
        string id = dto?.Id ?? "без Id";
        string message = exception.Message.Replace(Environment.NewLine, " ");
        return $"DTO №{index + 1} (Id: {id}): {message}";
    }
}
