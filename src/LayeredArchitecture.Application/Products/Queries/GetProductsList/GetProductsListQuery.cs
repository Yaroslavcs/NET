using LayeredArchitecture.Application.Common.Interfaces;

namespace LayeredArchitecture.Application.Products.Queries.GetProductsList;

public record GetProductsListQuery : IQuery<ProductsListVm>
{
    public string? Category { get; init; }
    public bool? IsActive { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}