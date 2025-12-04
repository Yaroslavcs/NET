using MongoDB.Bson;
using MongoDB.Driver;
using LayeredArchitecture.Domain.Entities;
using LayeredArchitecture.Domain.Interfaces;

namespace LayeredArchitecture.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;
    
    public Repository(IMongoCollection<T> collection)
    {
        _collection = collection;
    }
    
    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(entity => entity.Id == id).FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<T?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(entity => entity.Id == id.ToString()).FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(_ => true).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }
    
    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(e => e.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }
    
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(entity => entity.Id == id, cancellationToken);
    }
    
    public async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(entity => entity.Id == id.ToString(), cancellationToken);
    }
    
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(entity => entity.Id == id, cancellationToken: cancellationToken);
        return count > 0;
    }
    
    public async Task<bool> ExistsAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(entity => entity.Id == id.ToString(), cancellationToken: cancellationToken);
        return count > 0;
    }
}