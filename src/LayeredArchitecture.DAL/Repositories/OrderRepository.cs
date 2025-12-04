using Microsoft.EntityFrameworkCore;
using LayeredArchitecture.Common.Entities;
using LayeredArchitecture.DAL.Data;
using LayeredArchitecture.DAL.Interfaces;

namespace LayeredArchitecture.DAL.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    private readonly BudMasterDbContext _context;

    public OrderRepository(BudMasterDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Customer)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(string status)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Customer)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<bool> UpdateStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        
        return await _context.SaveChangesAsync() > 0;
    }

    // Override GetByIdAsync to include related entities
    public override async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    // Override GetAllAsync to include related entities
    public override async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}