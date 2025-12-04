using MongoDB.Driver;
using MongoDB.Driver.Linq;
using LayeredArchitecture.Domain.Entities;
using LayeredArchitecture.Domain.Interfaces;

namespace LayeredArchitecture.Infrastructure.Persistence.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(IMongoCollection<Order> collection) : base(collection)
    {
    }
    
    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(o => o.CustomerId == customerId).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(o => o.Status == status).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var results = await _collection.Find(o => o.OrderDate >= startDate && o.OrderDate <= endDate).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(o => o.OrderNumber == orderNumber).FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Order>> GetByTotalAmountRangeAsync(decimal minAmount, decimal maxAmount, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Order>.Filter.Gte(o => o.TotalAmount.Amount, minAmount) & 
                     Builders<Order>.Filter.Lte(o => o.TotalAmount.Amount, maxAmount);
        var results = await _collection.Find(filter).ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }
    
    public async Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Gte(o => o.OrderDate, startDate),
            Builders<Order>.Filter.Lte(o => o.OrderDate, endDate),
            Builders<Order>.Filter.Eq(o => o.Status, OrderStatus.Completed)
        );
        
        var orders = await _collection.Find(filter).ToListAsync(cancellationToken);
        return orders.Sum(o => o.TotalAmount.Amount);
    }
    
    public async Task<int> GetOrderCountByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        return (int)await _collection.CountDocumentsAsync(o => o.Status == status, cancellationToken: cancellationToken);
    }
    
    public async Task UpdateOrderStatusAsync(string orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var update = Builders<Order>.Update.Set(o => o.Status, newStatus);
        await _collection.UpdateOneAsync(o => o.Id == orderId, update, cancellationToken: cancellationToken);
    }
}