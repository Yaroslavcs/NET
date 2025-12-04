using Grpc.Core;
using LayeredArchitecture.API.Grpc;
using LayeredArchitecture.API.Services.Caching;
using LayeredArchitecture.API.Telemetry;
using LayeredArchitecture.Application.Interfaces;
using LayeredArchitecture.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace LayeredArchitecture.API.Services.GrpcServices;

public class ProductGrpcService : ProductService.ProductServiceBase
{
    private readonly IProductService _productService;
    private readonly IDistributedCacheService _cacheService;
    private readonly ILogger<ProductGrpcService> _logger;

    public ProductGrpcService(
        IProductService productService,
        IDistributedCacheService cacheService,
        ILogger<ProductGrpcService> logger)
    {
        _productService = productService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public override async Task<CachedProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetProduct");
        try
        {
            var cacheKey = $"product:{request.Id}";
            
            // Try to get from cache first
            var cachedProduct = await _cacheService.GetAsync<ProductResponse>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogDebug("Product {ProductId} found in cache", request.Id);
                
                return new CachedProductResponse
                {
                    Product = cachedProduct,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var product = await _productService.GetByIdAsync(request.Id);
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            var productResponse = MapToProductResponse(product);
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, productResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedProductResponse
            {
                Product = productResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CachedProductsResponse> GetAllProducts(GetAllProductsRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetAllProducts");
        try
        {
            var cacheKey = $"products:page:{request.Page}:size:{request.PageSize}";
            
            // Try to get from cache first
            var cachedProducts = await _cacheService.GetAsync<ProductsResponse>(cacheKey);
            if (cachedProducts != null)
            {
                _logger.LogDebug("Products page {Page} found in cache", request.Page);
                
                return new CachedProductsResponse
                {
                    Products = cachedProducts,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var products = await _productService.GetAllAsync(request.Page, request.PageSize);
            var productsResponse = new ProductsResponse
            {
                Products = { products.Select(MapToProductResponse) },
                TotalCount = products.Count(),
                Page = request.Page,
                PageSize = request.PageSize
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, productsResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedProductsResponse
            {
                Products = productsResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<ProductResponse> CreateProduct(CreateProductRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.CreateProduct");
        try
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                SKU = request.Sku,
                StockQuantity = request.StockQuantity
            };

            var createdProduct = await _productService.CreateAsync(product);
            var response = MapToProductResponse(createdProduct);

            // Invalidate related caches
            await InvalidateProductCaches();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<ProductResponse> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.UpdateProduct");
        try
        {
            var existingProduct = await _productService.GetByIdAsync(request.Id);
            if (existingProduct == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            existingProduct.Name = request.Name;
            existingProduct.Description = request.Description;
            existingProduct.Price = request.Price;
            existingProduct.SKU = request.Sku;
            existingProduct.StockQuantity = request.StockQuantity;

            var updatedProduct = await _productService.UpdateAsync(existingProduct);
            var response = MapToProductResponse(updatedProduct);

            // Invalidate related caches
            await InvalidateProductCaches();
            await _cacheService.RemoveAsync($"product:{request.Id}");

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.DeleteProduct");
        try
        {
            var result = await _productService.DeleteAsync(request.Id);
            if (!result)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            // Invalidate related caches
            await InvalidateProductCaches();
            await _cacheService.RemoveAsync($"product:{request.Id}");

            return new DeleteProductResponse
            {
                Success = true,
                Message = $"Product {request.Id} deleted successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private ProductResponse MapToProductResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Sku = product.SKU,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private async Task InvalidateProductCaches()
    {
        try
        {
            // Remove all product list caches
            await _cacheService.RemoveByPrefixAsync("products:page:");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating product caches");
        }
    }
}