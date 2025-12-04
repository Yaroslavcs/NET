using Microsoft.EntityFrameworkCore;
using LayeredArchitecture.Common.Entities;
using LayeredArchitecture.DAL.Data;
using LayeredArchitecture.DAL.Interfaces;

namespace LayeredArchitecture.DAL.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    private readonly BudMasterDbContext _context;

    public ProductRepository(BudMasterDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        return await _context.Products
            .Where(p => p.Category == category && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string name)
    {
        return await _context.Products
            .Where(p => p.Name.Contains(name) && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<bool> UpdateStockAsync(int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return false;

        product.StockQuantity = quantity;
        product.UpdatedAt = DateTime.UtcNow;
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<Product?> GetByNameAsync(string name)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, decimal? minPrice, decimal? maxPrice)
    {
        var query = _context.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => 
                p.Name.Contains(searchTerm) || 
                p.Description.Contains(searchTerm) ||
                p.Category.Contains(searchTerm));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}