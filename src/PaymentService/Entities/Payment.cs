using Loft.Common.Enums;

namespace PaymentService.Entities;

public class Payment
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime PaymentDate { get; set; }
}