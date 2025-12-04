using LayeredArchitecture.DAL.Data;
using LayeredArchitecture.DAL.Interfaces;
using LayeredArchitecture.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LayeredArchitecture.DAL;

public class UnitOfWork : IUnitOfWork
{
    private readonly BudMasterDbContext _context;
    private bool _disposed;

    private ICustomerRepository? _customerRepository;
    private IOrderRepository? _orderRepository;
    private IProductRepository? _productRepository;
    private IPaymentRepository? _paymentRepository;

    public UnitOfWork(BudMasterDbContext context)
    {
        _context = context;
    }

    public ICustomerRepository Customers
    {
        get
        {
            if (_customerRepository == null)
            {
                _customerRepository = new CustomerRepository(_context);
            }
            return _customerRepository;
        }
    }

    public IOrderRepository Orders
    {
        get
        {
            if (_orderRepository == null)
            {
                _orderRepository = new OrderRepository(_context);
            }
            return _orderRepository;
        }
    }

    public IProductRepository Products
    {
        get
        {
            if (_productRepository == null)
            {
                _productRepository = new ProductRepository(_context);
            }
            return _productRepository;
        }
    }

    public IPaymentRepository Payments
    {
        get
        {
            if (_paymentRepository == null)
            {
                _paymentRepository = new PaymentRepository(_context);
            }
            return _paymentRepository;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            await _context.Database.CommitTransactionAsync();
        }
        catch
        {
            await _context.Database.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RollbackAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    ~UnitOfWork()
    {
        Dispose(false);
    }
}