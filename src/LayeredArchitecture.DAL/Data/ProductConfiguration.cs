using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Data;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.Description)
            .HasMaxLength(500);
        
        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(p => p.StockQuantity)
            .IsRequired()
            .HasDefaultValue(0);
        
        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");
        
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Configure index for category
        builder.HasIndex(p => p.Category);
        
        // Configure index for name
        builder.HasIndex(p => p.Name);
        
        // Configure relationship with OrderItems
        builder.HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}