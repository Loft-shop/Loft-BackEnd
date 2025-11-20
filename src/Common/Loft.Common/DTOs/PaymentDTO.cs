using Loft.Common.Enums;

namespace Loft.Common.DTOs;

public record PaymentDTO(
    long Id,
    long OrderId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime PaymentDate,
    string? TransactionId
);

public record CreatePaymentDTO(
    long OrderId,
    decimal Amount,
    PaymentMethod Method
);

public record UpdatePaymentStatusDTO(
    long PaymentId,
    PaymentStatus Status,
    string? TransactionId
);
