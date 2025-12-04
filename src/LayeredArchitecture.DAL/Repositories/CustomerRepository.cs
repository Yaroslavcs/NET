using Microsoft.EntityFrameworkCore;
using LayeredArchitecture.Common.Entities;
using LayeredArchitecture.DAL.Data;
using LayeredArchitecture.DAL.Interfaces;

namespace LayeredArchitecture.DAL.Repositories;

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    private readonly BudMasterDbContext _context;

    public CustomerRepository(BudMasterDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string searchTerm)
    {
        return await _context.Customers
            .Where(c => c.IsActive && (
                c.FirstName.Contains(searchTerm) ||
                c.LastName.Contains(searchTerm) ||
                c.Email.Contains(searchTerm) ||
                c.Phone.Contains(searchTerm) ||
                c.City.Contains(searchTerm) ||
                c.Country.Contains(searchTerm)
            ))
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }
}