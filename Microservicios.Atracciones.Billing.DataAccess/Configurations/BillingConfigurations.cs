using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microservicios.Atracciones.Billing.DataAccess.Entities;

namespace Microservicios.Atracciones.Billing.DataAccess.Configurations;

public class PaymentMethodTypeConfiguration : IEntityTypeConfiguration<PaymentMethodType>
{
    public void Configure(EntityTypeBuilder<PaymentMethodType> builder)
    {
        builder.ToTable("payment_method_type");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(30).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasData(
            new PaymentMethodType { Id = 1, Name = "Card" },
            new PaymentMethodType { Id = 2, Name = "Transfer" },
            new PaymentMethodType { Id = 3, Name = "Cash" },
            new PaymentMethodType { Id = 4, Name = "PayPal" },
            new PaymentMethodType { Id = 5, Name = "Crypto" }
        );
    }
}

public class PaymentStatusTypeConfiguration : IEntityTypeConfiguration<PaymentStatusType>
{
    public void Configure(EntityTypeBuilder<PaymentStatusType> builder)
    {
        builder.ToTable("payment_status_type");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasData(
            new PaymentStatusType { Id = 1, Name = "Pending" },
            new PaymentStatusType { Id = 2, Name = "Succeeded" },
            new PaymentStatusType { Id = 3, Name = "Failed" },
            new PaymentStatusType { Id = 4, Name = "Refunded" },
            new PaymentStatusType { Id = 5, Name = "Disputed" }
        );
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CorrelationId);
        builder.Property(p => p.BookingId).IsRequired();
        builder.Property(p => p.PaymentMethodId).IsRequired();
        builder.Property(p => p.StatusId).IsRequired().HasDefaultValue((short)1);

        builder.Property(p => p.Amount).HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.CurrencyCode).HasMaxLength(3).IsFixedLength().HasDefaultValue("USD");
        builder.Property(p => p.TransactionId).HasMaxLength(100);

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasOne(p => p.PaymentMethod)
               .WithMany(pm => pm.Payments)
               .HasForeignKey(p => p.PaymentMethodId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Status)
               .WithMany(ps => ps.Payments)
               .HasForeignKey(p => p.StatusId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.BookingId).IsRequired();
        builder.Property(i => i.InvoiceNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        
        builder.Property(i => i.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(i => i.TaxId).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Total).HasPrecision(12, 2).IsRequired();
        builder.Property(i => i.CurrencyCode).HasMaxLength(3).IsFixedLength().HasDefaultValue("USD");
        
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasMany(i => i.Details)
               .WithOne(d => d.Invoice)
               .HasForeignKey(d => d.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvoiceDetailConfiguration : IEntityTypeConfiguration<InvoiceDetail>
{
    public void Configure(EntityTypeBuilder<InvoiceDetail> builder)
    {
        builder.ToTable("invoice_detail");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Description).HasMaxLength(255).IsRequired();
        builder.Property(d => d.Quantity).IsRequired();
        builder.Property(d => d.UnitPrice).HasPrecision(12, 2).IsRequired();
        builder.Property(d => d.TotalItem).HasPrecision(12, 2).IsRequired();
    }
}

public class BillingAuditLogConfiguration : IEntityTypeConfiguration<BillingAuditLog>
{
    public void Configure(EntityTypeBuilder<BillingAuditLog> builder)
    {
        builder.ToTable("billing_audit_log");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.CorrelationId);
        builder.Property(l => l.TableName).HasMaxLength(100);
        builder.Property(l => l.RecordId);
        builder.Property(l => l.Action).HasMaxLength(10);
        builder.Property(l => l.ChangedAt).HasDefaultValueSql("NOW()");
        builder.Property(l => l.OldValues).HasColumnType("jsonb");
        builder.Property(l => l.NewValues).HasColumnType("jsonb");
    }
}
