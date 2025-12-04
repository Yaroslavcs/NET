using Microsoft.EntityFrameworkCore;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Data;

public class BudMasterDbContext : DbContext
{
    public BudMasterDbContext(DbContextOptions<BudMasterDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Customers
        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1234567890",
                Address = "123 Main St",
                City = "New York",
                PostalCode = "10001",
                Country = "USA",
                CreatedAt = new DateTime(2024, 1, 1),
                IsActive = true
            },
            new Customer
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Phone = "+0987654321",
                Address = "456 Oak Ave",
                City = "Los Angeles",
                PostalCode = "90001",
                Country = "USA",
                CreatedAt = new DateTime(2024, 1, 15),
                IsActive = true
            }
        );

        // Seed Products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Laptop",
                Description = "High-performance laptop",
                Price = 999.99m,
                StockQuantity = 50,
                Category = "Electronics",
                CreatedAt = new DateTime(2024, 1, 1),
                IsActive = true
            },
            new Product
            {
                Id = 2,
                Name = "Smartphone",
                Description = "Latest smartphone model",
                Price = 699.99m,
                StockQuantity = 100,
                Category = "Electronics",
                CreatedAt = new DateTime(2024, 1, 1),
                IsActive = true
            },
            new Product
            {
                Id = 3,
                Name = "Headphones",
                Description = "Wireless headphones",
                Price = 199.99m,
                StockQuantity = 75,
                Category = "Electronics",
                CreatedAt = new DateTime(2024, 1, 1),
                IsActive = true
            }
        );

        // Seed Orders
        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = 1,
                CustomerId = 1,
                OrderDate = new DateTime(2024, 2, 1),
                TotalAmount = 1199.98m,
                Status = "Completed",
                CreatedAt = new DateTime(2024, 2, 1)
            },
            new Order
            {
                Id = 2,
                CustomerId = 2,
                OrderDate = new DateTime(2024, 2, 15),
                TotalAmount = 699.99m,
                Status = "Pending",
                CreatedAt = new DateTime(2024, 2, 15)
            }
        );

        // Seed OrderItems
        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem
            {
                Id = 1,
                OrderId = 1,
                ProductId = 1,
                Quantity = 1,
                UnitPrice = 999.99m
            },
            new OrderItem
            {
                Id = 2,
                OrderId = 1,
                ProductId = 3,
                Quantity = 1,
                UnitPrice = 199.99m
            },
            new OrderItem
            {
                Id = 3,
                OrderId = 2,
                ProductId = 2,
                Quantity = 1,
                UnitPrice = 699.99m
            }
        );
    }
}