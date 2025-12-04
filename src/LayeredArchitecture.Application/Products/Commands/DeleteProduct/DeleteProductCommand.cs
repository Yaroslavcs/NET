using LayeredArchitecture.Application.Common.Interfaces;

namespace LayeredArchitecture.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand : ICommand
{
    public string Id { get; init; } = string.Empty;
}