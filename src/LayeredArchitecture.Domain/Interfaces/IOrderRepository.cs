using LayeredArchitecture.Domain.Entities;

namespace LayeredArchitecture.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByTotalAmountRangeAsync(decimal minAmount, decimal maxAmount, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<int> GetOrderCountByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(string orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);
}