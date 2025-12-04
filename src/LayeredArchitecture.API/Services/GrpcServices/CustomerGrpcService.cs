using Grpc.Core;
using LayeredArchitecture.API.Grpc;
using LayeredArchitecture.API.Services.Caching;
using LayeredArchitecture.API.Telemetry;
using LayeredArchitecture.Application.Interfaces;
using LayeredArchitecture.Domain.Entities;

namespace LayeredArchitecture.API.Services.GrpcServices;

public class CustomerGrpcService : CustomerService.CustomerServiceBase
{
    private readonly ICustomerService _customerService;
    private readonly IDistributedCacheService _cacheService;
    private readonly ILogger<CustomerGrpcService> _logger;

    public CustomerGrpcService(
        ICustomerService customerService,
        IDistributedCacheService cacheService,
        ILogger<CustomerGrpcService> logger)
    {
        _customerService = customerService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public override async Task<CachedCustomerResponse> GetCustomer(GetCustomerRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetCustomer");
        try
        {
            var cacheKey = $"customer:{request.Id}";
            
            // Try to get from cache first
            var cachedCustomer = await _cacheService.GetAsync<CustomerResponse>(cacheKey);
            if (cachedCustomer != null)
            {
                _logger.LogDebug("Customer {CustomerId} found in cache", request.Id);
                
                return new CachedCustomerResponse
                {
                    Customer = cachedCustomer,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var customer = await _customerService.GetByIdAsync(request.Id);
            if (customer == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Customer {request.Id} not found"));
            }

            var customerResponse = MapToCustomerResponse(customer);
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, customerResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedCustomerResponse
            {
                Customer = customerResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CachedCustomersResponse> GetAllCustomers(GetAllCustomersRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetAllCustomers");
        try
        {
            var cacheKey = $"customers:page:{request.Page}:size:{request.PageSize}";
            
            // Try to get from cache first
            var cachedCustomers = await _cacheService.GetAsync<CustomersResponse>(cacheKey);
            if (cachedCustomers != null)
            {
                _logger.LogDebug("Customers page {Page} found in cache", request.Page);
                
                return new CachedCustomersResponse
                {
                    Customers = cachedCustomers,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var customers = await _customerService.GetAllAsync(request.Page, request.PageSize);
            var customersResponse = new CustomersResponse
            {
                Customers = { customers.Select(MapToCustomerResponse) },
                TotalCount = customers.Count(),
                Page = request.Page,
                PageSize = request.PageSize
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, customersResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedCustomersResponse
            {
                Customers = customersResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all customers");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CachedCustomersResponse> GetCustomersByCity(GetCustomersByCityRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.GetCustomersByCity");
        try
        {
            var cacheKey = $"customers:city:{request.City}:page:{request.Page}:size:{request.PageSize}";
            
            // Try to get from cache first
            var cachedCustomers = await _cacheService.GetAsync<CustomersResponse>(cacheKey);
            if (cachedCustomers != null)
            {
                _logger.LogDebug("Customers by city {City} page {Page} found in cache", request.City, request.Page);
                
                return new CachedCustomersResponse
                {
                    Customers = cachedCustomers,
                    CacheMetadata = new shared.CacheMetadata
                    {
                        IsCached = true,
                        CacheKey = cacheKey,
                        CacheTtlSeconds = 300,
                        CacheSource = "memory"
                    }
                };
            }

            // If not in cache, get from service
            var customers = await _customerService.GetByCityAsync(request.City, request.Page, request.PageSize);
            var customersResponse = new CustomersResponse
            {
                Customers = { customers.Select(MapToCustomerResponse) },
                TotalCount = customers.Count(),
                Page = request.Page,
                PageSize = request.PageSize
            };
            
            // Cache the result
            await _cacheService.SetAsync(cacheKey, customersResponse, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            return new CachedCustomersResponse
            {
                Customers = customersResponse,
                CacheMetadata = new shared.CacheMetadata
                {
                    IsCached = false,
                    CacheKey = cacheKey,
                    CacheTtlSeconds = 300,
                    CacheSource = "database"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers by city {City}", request.City);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CustomerResponse> CreateCustomer(CreateCustomerRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.CreateCustomer");
        try
        {
            var customer = new Customer
            {
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
                }
            };

            var createdCustomer = await _customerService.CreateAsync(customer);
            var response = MapToCustomerResponse(createdCustomer);

            // Invalidate related caches
            await InvalidateCustomerCaches();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<CustomerResponse> UpdateCustomer(UpdateCustomerRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.UpdateCustomer");
        try
        {
            var existingCustomer = await _customerService.GetByIdAsync(request.Id);
            if (existingCustomer == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Customer {request.Id} not found"));
            }

            existingCustomer.FirstName = request.FirstName;
            existingCustomer.LastName = request.LastName;
            existingCustomer.Email = request.Email;
            existingCustomer.Phone = request.Phone;
            existingCustomer.Address = new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                State = request.Address.State,
                ZipCode = request.Address.ZipCode,
                Country = request.Address.Country
            };

            var updatedCustomer = await _customerService.UpdateAsync(existingCustomer);
            var response = MapToCustomerResponse(updatedCustomer);

            // Invalidate related caches
            await InvalidateCustomerCaches();
            await _cacheService.RemoveAsync($"customer:{request.Id}");

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<DeleteCustomerResponse> DeleteCustomer(DeleteCustomerRequest request, ServerCallContext context)
    {
        var activity = ApiActivitySource.Source.StartActivity("gRPC.DeleteCustomer");
        try
        {
            var result = await _customerService.DeleteAsync(request.Id);
            if (!result)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Customer {request.Id} not found"));
            }

            // Invalidate related caches
            await InvalidateCustomerCaches();
            await _cacheService.RemoveAsync($"customer:{request.Id}");

            return new DeleteCustomerResponse
            {
                Success = true,
                Message = $"Customer {request.Id} deleted successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private CustomerResponse MapToCustomerResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = new Address
            {
                Street = customer.Address?.Street ?? "",
                City = customer.Address?.City ?? "",
                State = customer.Address?.State ?? "",
                ZipCode = customer.Address?.ZipCode ?? "",
                Country = customer.Address?.Country ?? ""
            },
            CreatedAt = customer.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            UpdatedAt = customer.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private async Task InvalidateCustomerCaches()
    {
        try
        {
            // Remove all customer list caches
            await _cacheService.RemoveByPrefixAsync("customers:");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating customer caches");
        }
    }
}