using LayeredArchitecture.BLL.DTOs;

namespace LayeredArchitecture.BLL.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto> CreateProductAsync(CreateProductDto productDto);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto productDto);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> ProductExistsAsync(int id);
    Task<bool> IsProductAvailableAsync(int productId, int quantity);
    Task<bool> UpdateProductStockAsync(int productId, int quantity);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string? searchTerm, decimal? minPrice, decimal? maxPrice);
}