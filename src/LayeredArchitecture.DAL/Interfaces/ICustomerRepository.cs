using System.Collections.Generic;
using System.Threading.Tasks;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Interfaces;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email);
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm);
}