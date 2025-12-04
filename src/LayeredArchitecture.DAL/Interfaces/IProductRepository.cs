using System.Collections.Generic;
using System.Threading.Tasks;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<IEnumerable<Product>> SearchByNameAsync(string name);
    Task<bool> UpdateStockAsync(int productId, int quantity);
    Task<Product?> GetByNameAsync(string name);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm, decimal? minPrice, decimal? maxPrice);
}