using LayeredArchitecture.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace LayeredArchitecture.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
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

        await _productRepository.DeleteAsync(objectId, cancellationToken);
    }
}