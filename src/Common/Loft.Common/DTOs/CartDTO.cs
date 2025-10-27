namespace Loft.Common.DTOs;

public record CartDTO(long Id, long CustomerId, DateTime CreatedAt, ICollection<CartItemDTO> CartItems);