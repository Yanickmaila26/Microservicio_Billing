using System.ComponentModel.DataAnnotations.Schema;

namespace Microservicios.Atracciones.Billing.DataAccess.Entities;

[Table("payment")]
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? CorrelationId { get; set; }

    public Guid BookingId { get; set; }

    public short PaymentMethodId { get; set; }

    public short StatusId { get; set; } = 1;            // 1 = Pending

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public string? TransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ══════════════════════════════════════════════════
    // NOT MAPPED BACKWARD COMPATIBILITY
    // ══════════════════════════════════════════════════
    [NotMapped]
    public string? TransactionExternalId
    {
        get => TransactionId;
        set => TransactionId = value;
    }

    [NotMapped]
    public string? GatewayResponse { get; set; }

    [NotMapped]
    public DateTime? PaidAt { get; set; }

    [NotMapped]
    public DateTime? RefundedAt { get; set; }

    [NotMapped]
    public string? RefundReason { get; set; }

    // ══════════════════════════════════════════════════
    // NAVIGATIONS
    // ══════════════════════════════════════════════════
    public virtual PaymentMethodType PaymentMethod { get; set; } = null!;
    public virtual PaymentStatusType Status { get; set; } = null!;
}
