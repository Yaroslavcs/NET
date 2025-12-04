using LayeredArchitecture.BLL.Services;
using LayeredArchitecture.BLL.Interfaces;
using LayeredArchitecture.DAL.Repositories;
using LayeredArchitecture.DAL.Interfaces;
using LayeredArchitecture.DAL;

namespace LayeredArchitecture.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register BLL services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        
        return services;
    }
    
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}