using monolith_version.Models.Enums;

namespace monolith_version.DTOs;

public record PaymentDto(long Id,long OrderId,decimal Amount,PaymentMethod Method,PaymentStatus Status,DateTime PaymentDate);