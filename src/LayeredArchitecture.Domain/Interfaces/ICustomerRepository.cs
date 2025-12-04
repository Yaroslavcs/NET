using LayeredArchitecture.Domain.Entities;

namespace LayeredArchitecture.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetByLoyaltyPointsRangeAsync(int minPoints, int maxPoints, CancellationToken cancellationToken = default);
    Task UpdateLoyaltyPointsAsync(string customerId, int newPoints, CancellationToken cancellationToken = default);
}