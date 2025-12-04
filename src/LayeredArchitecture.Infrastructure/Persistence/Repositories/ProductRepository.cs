using MongoDB.Driver;
using MongoDB.Driver.Linq;
using LayeredArchitecture.Domain.Entities;
using LayeredArchitecture.Domain.Interfaces;

namespace LayeredArchitecture.Infrastructure.Persistence.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(IMongoCollection<Product> collection) : base(collection)
    {
    }
    
    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(p => p.Category == category).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(p => p.IsActive).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<IReadOnlyList<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(p => p.Price.Amount >= minPrice && p.Price.Amount <= maxPrice).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<IReadOnlyList<Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
        var results = await _collection.Find(filter).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<IReadOnlyList<Product>> GetByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        var tagList = tags.ToList();
        var filter = Builders<Product>.Filter.All(p => p.Tags, tagList);
        var results = await _collection.Find(filter).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(p => p.Sku == sku, cancellationToken: cancellationToken);
        return count > 0;
    }
    
    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(p => p.Sku == sku).FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task UpdateStockAsync(string productId, int newQuantity, CancellationToken cancellationToken = default)
    {
        var update = Builders<Product>.Update.Set(p => p.StockQuantity, newQuantity);
        await _collection.UpdateOneAsync(p => p.Id == productId, update, cancellationToken: cancellationToken);
    }
    
    public async Task<bool> ProductNameExistsAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Name, name);
        if (!string.IsNullOrEmpty(excludeId))
        {
            filter &= Builders<Product>.Filter.Ne(p => p.Id, excludeId);
        }
        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }
}