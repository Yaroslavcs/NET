using Microsoft.EntityFrameworkCore;
using LayeredArchitecture.Common.Entities;
using LayeredArchitecture.DAL.Data;
using LayeredArchitecture.DAL.Interfaces;

namespace LayeredArchitecture.DAL.Repositories;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(BudMasterDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.Payments
            .Include(p => p.Order)
            .Where(p => p.Order.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetPendingPaymentsAsync()
    {
        return await _context.Payments
            .Include(p => p.Order)
            .Where(p => p.Status == "Pending")
            .ToListAsync();
    }

    public async Task<bool> UpdatePaymentStatusAsync(int paymentId, string status)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            return false;

        payment.Status = status;
        payment.UpdatedAt = DateTime.UtcNow;
        
        return await _context.SaveChangesAsync() > 0;
    }
}