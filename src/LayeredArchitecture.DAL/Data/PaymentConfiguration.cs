using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LayeredArchitecture.Common.Entities;

namespace LayeredArchitecture.DAL.Data;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.OrderId)
            .IsRequired();
        
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);
        
        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(p => p.PaymentDate)
            .IsRequired();
        
        builder.Property(p => p.TransactionId)
            .HasMaxLength(100);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()");
        
        // Configure relationship with Order
        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure index for OrderId
        builder.HasIndex(p => p.OrderId);
        
        // Configure index for Status
        builder.HasIndex(p => p.Status);
        
        // Configure index for PaymentDate
        builder.HasIndex(p => p.PaymentDate);
    }
}