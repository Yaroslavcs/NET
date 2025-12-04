using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Data;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.CustomerId)
            .IsRequired();
        
        builder.Property(o => o.OrderDate)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");
        
        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");
        
        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");
        
        // Configure index for CustomerId
        builder.HasIndex(o => o.CustomerId);
        
        // Configure index for OrderDate
        builder.HasIndex(o => o.OrderDate);
        
        // Configure index for Status
        builder.HasIndex(o => o.Status);
        
        // Configure relationship with Customer
        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure relationship with OrderItems
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}