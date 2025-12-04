using System;
using System.Data;
using System.Threading.Tasks;

namespace LayeredArchitecture.DAL.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    IProductRepository Products { get; }
    IPaymentRepository Payments { get; }
    
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    Task CommitAsync();
    Task RollbackAsync();
    Task<int> SaveChangesAsync();
}