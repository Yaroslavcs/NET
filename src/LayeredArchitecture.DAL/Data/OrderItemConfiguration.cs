using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Data;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        
        builder.HasKey(oi => oi.Id);
        
        builder.Property(oi => oi.OrderId)
            .IsRequired();
        
        builder.Property(oi => oi.ProductId)
            .IsRequired();
        
        builder.Property(oi => oi.Quantity)
            .IsRequired()
            .HasDefaultValue(1);
        
        builder.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);
        
        // Configure composite index for OrderId and ProductId
        builder.HasIndex(oi => new { oi.OrderId, oi.ProductId })
            .IsUnique();
        
        // Configure relationship with Order
        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure relationship with Product
        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}