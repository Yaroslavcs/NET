using Grpc.Core;
using LayeredArchitecture.API.Grpc;
using LayeredArchitecture.Application.Common.Interfaces;
using LayeredArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LayeredArchitecture.API.Services;

public class CustomerService : CustomerService.CustomerServiceBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IApplicationDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<CustomerResponse> GetCustomer(GetCustomerRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting customer with ID: {CustomerId}", request.Id);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (customer == null)
        {
            _logger.LogWarning("Customer with ID: {CustomerId} not found", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Customer with ID {request.Id} not found"));
        }

        return new CustomerResponse
        {
            Id = customer.Id.ToString(),
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone ?? string.Empty,
            Address = new AddressMessage
            {
                Street = customer.Address?.Street ?? string.Empty,
                City = customer.Address?.City ?? string.Empty,
                State = customer.Address?.State ?? string.Empty,
                ZipCode = customer.Address?.ZipCode ?? string.Empty,
                Country = customer.Address?.Country ?? string.Empty
            },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.UpdatedAt.ToUniversalTime())
        };
    }

    public override async Task<CustomersResponse> GetAllCustomers(GetAllCustomersRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting all customers with page: {Page}, pageSize: {PageSize}", 
            request.Page, request.PageSize);

        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(c => 
                c.FirstName.Contains(request.SearchTerm) || 
                c.LastName.Contains(request.SearchTerm) || 
                c.Email.Contains(request.SearchTerm));
        }

        var totalCount = await query.CountAsync(context.CancellationToken);
        var customers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(context.CancellationToken);

        var response = new CustomersResponse
        {
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        response.Customers.AddRange(customers.Select(customer => new CustomerResponse
        {
            Id = customer.Id.ToString(),
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone ?? string.Empty,
            Address = new AddressMessage
            {
                Street = customer.Address?.Street ?? string.Empty,
                City = customer.Address?.City ?? string.Empty,
                State = customer.Address?.State ?? string.Empty,
                ZipCode = customer.Address?.ZipCode ?? string.Empty,
                Country = customer.Address?.Country ?? string.Empty
            },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.UpdatedAt.ToUniversalTime())
        }));

        return response;
    }

    public override async Task<CustomerResponse> CreateCustomer(CreateCustomerRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Creating new customer: {FirstName} {LastName}", request.FirstName, request.LastName);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                State = request.Address.State,
                ZipCode = request.Address.ZipCode,
                Country = request.Address.Country
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Customer created successfully with ID: {CustomerId}", customer.Id);

        return new CustomerResponse
        {
            Id = customer.Id.ToString(),
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone ?? string.Empty,
            Address = new AddressMessage
            {
                Street = customer.Address?.Street ?? string.Empty,
                City = customer.Address?.City ?? string.Empty,
                State = customer.Address?.State ?? string.Empty,
                ZipCode = customer.Address?.ZipCode ?? string.Empty,
                Country = customer.Address?.Country ?? string.Empty
            },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.UpdatedAt.ToUniversalTime())
        };
    }

    public override async Task<CustomerResponse> UpdateCustomer(UpdateCustomerRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Updating customer with ID: {CustomerId}", request.Id);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (customer == null)
        {
            _logger.LogWarning("Customer with ID: {CustomerId} not found for update", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Customer with ID {request.Id} not found"));
        }

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Address = new Address
        {
            Street = request.Address.Street,
            City = request.Address.City,
            State = request.Address.State,
            ZipCode = request.Address.ZipCode,
            Country = request.Address.Country
        };
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Customer updated successfully with ID: {CustomerId}", customer.Id);

        return new CustomerResponse
        {
            Id = customer.Id.ToString(),
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone ?? string.Empty,
            Address = new AddressMessage
            {
                Street = customer.Address?.Street ?? string.Empty,
                City = customer.Address?.City ?? string.Empty,
                State = customer.Address?.State ?? string.Empty,
                ZipCode = customer.Address?.ZipCode ?? string.Empty,
                Country = customer.Address?.Country ?? string.Empty
            },
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(customer.UpdatedAt.ToUniversalTime())
        };
    }

    public override async Task<DeleteCustomerResponse> DeleteCustomer(DeleteCustomerRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Deleting customer with ID: {CustomerId}", request.Id);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(request.Id), context.CancellationToken);

        if (customer == null)
        {
            _logger.LogWarning("Customer with ID: {CustomerId} not found for deletion", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, $"Customer with ID {request.Id} not found"));
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Customer deleted successfully with ID: {CustomerId}", customer.Id);

        return new DeleteCustomerResponse { Success = true };
    }
}