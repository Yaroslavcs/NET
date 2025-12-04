using LayeredArchitecture.Application.Common.Interfaces;
using LayeredArchitecture.Domain.ValueObjects;

namespace LayeredArchitecture.Application.Products.Commands.CreateProduct;

public record CreateProductCommand : ICommand<string>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public int StockQuantity { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
}