using LayeredArchitecture.Application.Common.Interfaces;

namespace LayeredArchitecture.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery : IQuery<ProductDto>
{
    public string Id { get; init; } = string.Empty;
}