using System.Collections.Generic;
using System.Threading.Tasks;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Payment>> GetPendingPaymentsAsync();
    Task<bool> UpdatePaymentStatusAsync(int paymentId, string status);
}