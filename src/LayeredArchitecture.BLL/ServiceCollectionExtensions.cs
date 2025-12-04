using LayeredArchitecture.BLL.Interfaces;
using LayeredArchitecture.BLL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LayeredArchitecture.BLL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        
        return services;
    }
}