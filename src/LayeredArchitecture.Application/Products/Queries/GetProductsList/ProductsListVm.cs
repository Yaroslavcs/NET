namespace LayeredArchitecture.Application.Products.Queries.GetProductsList;

public record ProductsListVm
{
    public IReadOnlyList<ProductDto> Products { get; init; } = new List<ProductDto>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

public record ProductDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public int StockQuantity { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<string> Tags { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}