using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microservicios.Atracciones.Billing.DataAccess.Entities;

[Table("billing_audit_log")]
public class BillingAuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? CorrelationId { get; set; }

    [MaxLength(100)]
    public string? TableName { get; set; }

    public Guid? RecordId { get; set; }

    [MaxLength(10)]
    public string? Action { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "jsonb")]
    public string? OldValues { get; set; }

    [Column(TypeName = "jsonb")]
    public string? NewValues { get; set; }
}
