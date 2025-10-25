using MonolithVersion.Enums;

namespace MonolithVersion.DTOs;

public record PaymentDTO(long Id,long OrderId,decimal Amount,PaymentMethod Method,PaymentStatus Status,DateTime PaymentDate);