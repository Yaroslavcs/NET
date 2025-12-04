using Microsoft.Extensions.Options;
using MongoDB.Driver;
using LayeredArchitecture.Domain.Entities;

namespace LayeredArchitecture.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    
    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }
    
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("products");
    public IMongoCollection<Customer> Customers => _database.GetCollection<Customer>("customers");
    public IMongoCollection<Order> Orders => _database.GetCollection<Order>("orders");
}