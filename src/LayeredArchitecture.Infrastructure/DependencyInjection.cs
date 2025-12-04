using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using LayeredArchitecture.Domain.Entities;
using LayeredArchitecture.Domain.Interfaces;
using LayeredArchitecture.Infrastructure.Persistence;
using LayeredArchitecture.Infrastructure.Persistence.Repositories;

namespace LayeredArchitecture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB settings
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        
        // Register MongoDB context
        services.AddSingleton<MongoDbContext>();
        
        // Register repositories
        services.AddScoped<IProductRepository>(provider =>
        {
            var context = provider.GetRequiredService<MongoDbContext>();
            return new ProductRepository(context.Products);
        });
        
        services.AddScoped<ICustomerRepository>(provider =>
        {
            var context = provider.GetRequiredService<MongoDbContext>();
            return new CustomerRepository(context.Customers);
        });
        
        services.AddScoped<IOrderRepository>(provider =>
        {
            var context = provider.GetRequiredService<MongoDbContext>();
            return new OrderRepository(context.Orders);
        });
        
        return services;
    }
}