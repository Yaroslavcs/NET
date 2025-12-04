using LayeredArchitecture.Domain.Interfaces;
using LayeredArchitecture.Domain.ValueObjects;
using MediatR;
using MongoDB.Bson;

namespace LayeredArchitecture.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(request.Id, out var objectId))
        {
            throw new ArgumentException("Invalid product ID format", nameof(request.Id));
        }

        var product = await _productRepository.GetByIdAsync(objectId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.Id} not found");
        }

        var price = new Money(request.Price, request.Currency);
        
        product.UpdateDetails(
            request.Name,
            request.Description,
            price,
            request.Category
        );

        product.UpdateStock(request.StockQuantity);

        await _productRepository.UpdateAsync(product, cancellationToken);
    }
}