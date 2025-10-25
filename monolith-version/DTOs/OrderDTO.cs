using monolith_version.Models.Enums;

namespace monolith_version.DTOs;

public record OrderDTO(long Id,long CustomerId,DateTime OrderDate,OrderStatus Status,decimal TotalAmount);