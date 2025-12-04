using LayeredArchitecture.Domain.Interfaces;
using LayeredArchitecture.Domain.Entities;
using LayeredArchitecture.Domain.ValueObjects;
using MediatR;
using MongoDB.Bson;

namespace LayeredArchitecture.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, string>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<string> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var price = new Money(request.Price, request.Currency);
        
        var product = new Product(
            request.Name,
            request.Description,
            price,
            request.StockQuantity,
            request.Category,
            request.Sku
        );

        // Add tags if provided
        foreach (var tag in request.Tags)
        {
            product.AddTag(tag);
        }

        await _productRepository.AddAsync(product, cancellationToken);
        
        return product.Id.ToString();
    }
}