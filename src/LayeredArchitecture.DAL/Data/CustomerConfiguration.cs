using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Data;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(c => c.Phone)
            .HasMaxLength(20);
        
        builder.Property(c => c.Address)
            .HasMaxLength(200);
        
        builder.Property(c => c.City)
            .HasMaxLength(50);
        
        builder.Property(c => c.PostalCode)
            .HasMaxLength(20);
        
        builder.Property(c => c.Country)
            .HasMaxLength(50);
        
        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");
        
        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Configure index for email (unique)
        builder.HasIndex(c => c.Email)
            .IsUnique();
        
        // Configure relationship with Orders
        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}