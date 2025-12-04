using LayeredArchitecture.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace LayeredArchitecture.Application.Products.Queries.GetProductsList;

public class GetProductsListQueryHandler : IRequestHandler<GetProductsListQuery, ProductsListVm>
{
    private readonly IProductRepository _productRepository;

    public GetProductsListQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductsListVm> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        // For now, we'll get all products and filter in memory
        // In a real implementation, you'd implement proper filtering at the repository level
        var allProducts = await _productRepository.GetAllAsync(cancellationToken);
        
        var query = allProducts.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(p => p.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchTerm) || 
                p.Description.ToLower().Contains(searchTerm) ||
                p.Sku.ToLower().Contains(searchTerm));
        }

        var filteredProducts = query.ToList();
        var totalCount = filteredProducts.Count;

        // Apply pagination
        var pagedProducts = filteredProducts
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var productDtos = pagedProducts.Select(product => new ProductDto
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
        }).ToList();

        return new ProductsListVm
        {
            Products = productDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            HasNextPage = request.PageNumber * request.PageSize < totalCount,
            HasPreviousPage = request.PageNumber > 1
        };
    }
}