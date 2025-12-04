using LayeredArchitecture.BLL.DTOs;

namespace LayeredArchitecture.BLL.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto customerDto);
    Task<CustomerDto?> UpdateCustomerAsync(int id, UpdateCustomerDto customerDto);
    Task<bool> DeleteCustomerAsync(int id);
    Task<bool> CustomerExistsAsync(int id);
    Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string? searchTerm);
}