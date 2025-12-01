using Loft.Common.DTOs;
using Loft.Common.Enums;

public class OrderDTO
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public long? ShippingAddressId { get; set; }
    public ShippingAddressDTO? ShippingAddress { get; set; }
    public ICollection<OrderItemDTO>? OrderItems { get; set; }
}
