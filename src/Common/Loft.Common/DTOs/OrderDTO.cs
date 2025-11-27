using System;
using System.Collections.Generic;
using Loft.Common.Enums;

namespace Loft.Common.DTOs;

public record OrderDTO(
    long Id,
    long CustomerId,
    DateTime OrderDate,
    OrderStatus Status,
    decimal TotalAmount,
    string? CustomerName = null,
    string? CustomerEmail = null,
    long? ShippingAddressId = null,
    ShippingAddressDTO? ShippingAddress = null,
    ICollection<OrderItemDTO>? OrderItems = null
);