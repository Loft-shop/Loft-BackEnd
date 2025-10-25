using MonolithVersion.Enums;

namespace MonolithVersion.DTOs;

public record OrderDTO(long Id,long CustomerId,DateTime OrderDate,OrderStatus Status,decimal TotalAmount);