using System;
using System.Collections.Generic;
using Loft.Common.Enums;

namespace OrderService.Entities;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime UpdatedDate { get; set; }
    
    // Данные покупателя
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    
    // Адрес доставки
    public long? ShippingAddressId { get; set; }
    
    // Сохранённые данные адреса доставки (на момент создания заказа)
    public string? ShippingAddress { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
    public string? ShippingRecipientName { get; set; }
    
    public ICollection<OrderItem>? OrderItems { get; set; }
}