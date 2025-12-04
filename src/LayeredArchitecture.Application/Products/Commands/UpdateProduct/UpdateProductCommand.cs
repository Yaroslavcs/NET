using LayeredArchitecture.Application.Common.Interfaces;

namespace LayeredArchitecture.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand : ICommand
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public int StockQuantity { get; init; }
    public string Category { get; init; } = string.Empty;
}