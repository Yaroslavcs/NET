using System.Collections.Generic;
using System.Threading.Tasks;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Order>> GetByStatusAsync(string status);
    Task<bool> UpdateStatusAsync(int orderId, string status);
}