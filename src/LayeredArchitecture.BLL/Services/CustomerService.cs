using AutoMapper;
using LayeredArchitecture.BLL.DTOs;
using LayeredArchitecture.BLL.Exceptions;
using LayeredArchitecture.BLL.Interfaces;
using LayeredArchitecture.Common.Entities;
using LayeredArchitecture.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace LayeredArchitecture.BLL.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CustomerService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
    {
        try
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            return _mapper.Map<IEnumerable<CustomerDto>>(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all customers");
            throw new BusinessException("An error occurred while retrieving customers", ex);
        }
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Customer ID must be greater than zero");
        }

        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            return customer == null ? null : _mapper.Map<CustomerDto>(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting customer with ID {CustomerId}", id);
            throw new BusinessException($"An error occurred while retrieving customer with ID {id}", ex);
        }
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto customerDto)
    {
        if (customerDto == null)
        {
            throw new ValidationException("Customer data cannot be null");
        }

        ValidateCustomerData(customerDto);

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var existingCustomer = await _unitOfWork.Customers.GetByEmailAsync(customerDto.Email);
            if (existingCustomer != null)
            {
                throw new BusinessException($"Customer with email '{customerDto.Email}' already exists");
            }

            var customer = _mapper.Map<Customer>(customerDto);
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Customer created successfully with ID {CustomerId}", customer.Id);
            return _mapper.Map<CustomerDto>(customer);
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while creating customer");
            throw new BusinessException("An error occurred while creating customer", ex);
        }
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(int id, UpdateCustomerDto customerDto)
    {
        if (id <= 0)
        {
            throw new ValidationException("Customer ID must be greater than zero");
        }

        if (customerDto == null)
        {
            throw new ValidationException("Customer data cannot be null");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var existingCustomer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (existingCustomer == null)
            {
                throw new NotFoundException("Customer", id);
            }

            if (!string.IsNullOrEmpty(customerDto.Email) && customerDto.Email != existingCustomer.Email)
            {
                var customerWithEmail = await _unitOfWork.Customers.GetByEmailAsync(customerDto.Email);
                if (customerWithEmail != null)
                {
                    throw new BusinessException($"Customer with email '{customerDto.Email}' already exists");
                }
            }

            _mapper.Map(customerDto, existingCustomer);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Customer updated successfully with ID {CustomerId}", id);
            return _mapper.Map<CustomerDto>(existingCustomer);
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while updating customer with ID {CustomerId}", id);
            throw new BusinessException($"An error occurred while updating customer with ID {id}", ex);
        }
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Customer ID must be greater than zero");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer == null)
            {
                throw new NotFoundException("Customer", id);
            }

            var customerOrders = await _unitOfWork.Orders.GetByCustomerIdAsync(id);
            if (customerOrders.Any())
            {
                throw new BusinessException("Cannot delete customer with existing orders");
            }

            await _unitOfWork.Customers.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Customer deleted successfully with ID {CustomerId}", id);
            return true;
        }
        catch (BusinessException)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error occurred while deleting customer with ID {CustomerId}", id);
            throw new BusinessException($"An error occurred while deleting customer with ID {id}", ex);
        }
    }

    public async Task<bool> CustomerExistsAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Customer ID must be greater than zero");
        }

        try
        {
            return await _unitOfWork.Customers.ExistsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if customer exists with ID {CustomerId}", id);
            throw new BusinessException($"An error occurred while checking customer existence with ID {id}", ex);
        }
    }

    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string? searchTerm)
    {
        try
        {
            var customers = await _unitOfWork.Customers.SearchAsync(searchTerm);
            return _mapper.Map<IEnumerable<CustomerDto>>(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching customers with term '{SearchTerm}'", searchTerm);
            throw new BusinessException("An error occurred while searching customers", ex);
        }
    }

    private void ValidateCustomerData(CreateCustomerDto customerDto)
    {
        if (string.IsNullOrWhiteSpace(customerDto.FirstName))
        {
            throw new ValidationException("First name is required");
        }

        if (string.IsNullOrWhiteSpace(customerDto.LastName))
        {
            throw new ValidationException("Last name is required");
        }

        if (string.IsNullOrWhiteSpace(customerDto.Email))
        {
            throw new ValidationException("Email is required");
        }

        if (!IsValidEmail(customerDto.Email))
        {
            throw new ValidationException("Invalid email format");
        }

        if (!string.IsNullOrEmpty(customerDto.Phone) && customerDto.Phone.Length > 20)
        {
            throw new ValidationException("Phone number cannot exceed 20 characters");
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}