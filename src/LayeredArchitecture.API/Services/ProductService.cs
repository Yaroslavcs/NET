using Grpc.Core;
using LayeredArchitecture.API.Grpc;
using LayeredArchitecture.Application.Common.Interfaces;
using LayeredArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LayeredArchitecture.API.Services;

public class ProductService : ProductService.ProductServiceBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly Services.Caching.ICacheService _cacheService;

    public ProductService(IApplicationDbContext context, ILogger<ProductService> logger, Services.Caching.ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", request.Id);

        var cacheKey = $"product:{request.Id}";
        
        // Try to get from cache first
        var cachedProduct = await _cacheService.GetAsync<ProductResponse>(cacheKey);
        if (cachedProduct != null)
        {
            _logger.LogInformation("Product {ProductId} found in cache", request.Id);
            return cachedProduct;
        }

        // If not in cache, get from database
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID {request.Id} not found"));
        }

        var response = new ProductResponse
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.UpdatedAt.ToUniversalTime())
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
        _logger.LogInformation("Product {ProductId} cached for 10 minutes", request.Id);

        return response;
    }

    public override async Task<ProductsResponse> GetAllProducts(GetAllProductsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting all products with page: {Page}, pageSize: {PageSize}", 
            request.Page, request.PageSize);

        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(p => p.Category.Contains(request.Category));
        }

        var totalCount = await query.CountAsync(context.CancellationToken);
        var products = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(context.CancellationToken);

        var response = new ProductsResponse
        {
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        response.Products.AddRange(products.Select(product => new ProductResponse
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.UpdatedAt.ToUniversalTime())
        }));

        return response;
    }

    public override async Task<ProductResponse> CreateProduct(CreateProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Creating new product: {ProductName}", request.Name);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);

        var response = new ProductResponse
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.UpdatedAt.ToUniversalTime())
        };

        // Cache the newly created product
        var cacheKey = $"product:{product.Id}";
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
        _logger.LogInformation("Newly created product {ProductId} cached for 10 minutes", product.Id);

        return response;
    }

    public override async Task<ProductResponse> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", request.Id);

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found for update", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID {request.Id} not found"));
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.Category = request.Category;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Product updated successfully with ID: {ProductId}", product.Id);

        var response = new ProductResponse
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(product.UpdatedAt.ToUniversalTime())
        };

        // Update cache with new values
        var cacheKey = $"product:{product.Id}";
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
        _logger.LogInformation("Product {ProductId} cache updated after modification", product.Id);

        return response;
    }

    public override async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", request.Id);

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found for deletion", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID {request.Id} not found"));
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Product deleted successfully with ID: {ProductId}", product.Id);

        // Remove from cache
        var cacheKey = $"product:{request.Id}";
        await _cacheService.RemoveAsync(cacheKey);
        _logger.LogInformation("Product {ProductId} removed from cache", request.Id);

        return new DeleteProductResponse { Success = true };
    }
}