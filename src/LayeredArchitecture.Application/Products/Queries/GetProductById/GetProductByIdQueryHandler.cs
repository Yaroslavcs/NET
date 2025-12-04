using LayeredArchitecture.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace LayeredArchitecture.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(request.Id, out var objectId))
        {
            throw new ArgumentException("Invalid product ID format", nameof(request.Id));
        }

        var product = await _productRepository.GetByIdAsync(objectId, cancellationToken);
        
        if (product == null)
        {
            return null;
        }

        return new ProductDto
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            Sku = product.Sku,
            IsActive = product.IsActive,
            Tags = product.Tags,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}