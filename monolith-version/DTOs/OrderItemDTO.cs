namespace monolith_version.DTOs;

public record OrderItemDto(long Id,long OrderId,long ProductId,int Quantity,decimal Price);