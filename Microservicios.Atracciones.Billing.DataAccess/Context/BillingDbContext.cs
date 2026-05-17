using Microsoft.EntityFrameworkCore;
using Microservicios.Atracciones.Billing.DataAccess.Entities;
using System.Text.Json;

namespace Microservicios.Atracciones.Billing.DataAccess.Context;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    // ══════════════════════════════════════════════════
    // PAGOS Y FACTURACIÓN
    // ══════════════════════════════════════════════════
    public DbSet<PaymentMethodType> PaymentMethodTypes { get; set; }
    public DbSet<PaymentStatusType> PaymentStatusTypes { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
    public DbSet<BillingAuditLog> BillingAuditLogs { get; set; }

    // ══════════════════════════════════════════════════
    // MODEL CREATING
    // ══════════════════════════════════════════════════
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        // Mapeo manual para resolver la inconsistencia de nombres en tu BD
        var tableMapping = new Dictionary<string, string>
        {
            { nameof(Payment), "payment" },
            { nameof(PaymentMethodType), "payment_method_type" },
            { nameof(PaymentStatusType), "payment_status_type" },
            { nameof(Invoice), "invoice" },
            { nameof(InvoiceDetail), "invoice_detail" },
            { nameof(BillingAuditLog), "billing_audit_log" }
        };

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var entityName = entity.ClrType.Name;
            if (tableMapping.TryGetValue(entityName, out var tableName))
            {
                entity.SetTableName(tableName);
            }

            // Mantenemos snake_case para las columnas porque esas sí parecen consistentes
            foreach (var property in entity.GetProperties())
            {
                var propName = ToSnakeCase(property.Name);
                property.SetColumnName(propName);
            }
        }
    }

    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        var auditLogs = GenerateAuditLogs();
        var result = await base.SaveChangesAsync(cancellationToken);
        
        if (auditLogs.Any())
        {
            BillingAuditLogs.AddRange(auditLogs);
            await base.SaveChangesAsync(cancellationToken);
        }
        
        return result;
    }

    private void StampAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null) createdAtProp.CurrentValue = now;
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null) updatedAtProp.CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null) updatedAtProp.CurrentValue = now;
            }
        }
    }

    private List<BillingAuditLog> GenerateAuditLogs()
    {
        var auditLogs = new List<BillingAuditLog>();
        var entries = ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is BillingAuditLog)
                continue;

            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditLog = new BillingAuditLog
            {
                Id = Guid.NewGuid(),
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                Action = entry.State switch
                {
                    EntityState.Added => "INSERT",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                },
                ChangedAt = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid()
            };

            var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            if (keyProperty != null && keyProperty.CurrentValue is Guid recordGuid)
            {
                auditLog.RecordId = recordGuid;
            }

            var oldDict = new Dictionary<string, object?>();
            var newDict = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;

                if (property.Metadata.IsShadowProperty() && !property.Metadata.IsForeignKey())
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newDict[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        oldDict[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldDict[propertyName] = property.OriginalValue;
                            newDict[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            auditLog.OldValues = oldDict.Any() ? JsonSerializer.Serialize(oldDict) : null;
            auditLog.NewValues = newDict.Any() ? JsonSerializer.Serialize(newDict) : null;

            auditLogs.Add(auditLog);
        }

        return auditLogs;
    }
}
