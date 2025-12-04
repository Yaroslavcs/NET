using MongoDB.Driver;
using MongoDB.Driver.Linq;
using LayeredArchitecture.Domain.Entities;
using LayeredArchitecture.Domain.Interfaces;

namespace LayeredArchitecture.Infrastructure.Persistence.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(IMongoCollection<Customer> collection) : base(collection)
    {
    }
    
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(c => c.Email == email).FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Regex(c => c.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
        var results = await _collection.Find(filter).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<IReadOnlyList<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(c => c.IsActive).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(c => c.Email == email, cancellationToken: cancellationToken);
        return count > 0;
    }
    
    public async Task<IReadOnlyList<Customer>> GetByLoyaltyPointsRangeAsync(int minPoints, int maxPoints, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Gte(c => c.LoyaltyPoints, minPoints) & 
                     Builders<Customer>.Filter.Lte(c => c.LoyaltyPoints, maxPoints);
        var results = await _collection.Find(filter).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task UpdateLoyaltyPointsAsync(string customerId, int newPoints, CancellationToken cancellationToken = default)
    {
        var update = Builders<Customer>.Update.Set(c => c.LoyaltyPoints, newPoints);
        await _collection.UpdateOneAsync(c => c.Id == customerId, update, cancellationToken: cancellationToken);
    }
    
    public async Task<IEnumerable<Customer>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(c => c.Address.City == city).ToListAsync(cancellationToken);
    }
    
    public async Task<bool> EmailExistsAsync(string email, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.Email, email);
        if (!string.IsNullOrEmpty(excludeId))
        {
            filter &= Builders<Customer>.Filter.Ne(c => c.Id, excludeId);
        }
        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }
}