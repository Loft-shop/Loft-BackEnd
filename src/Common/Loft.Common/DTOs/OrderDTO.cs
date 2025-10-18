using System;
using Loft.Common.Enums;

namespace Loft.Common.DTOs;

public record OrderDTO(long Id,long CustomerId,DateTime OrderDate,OrderStatus Status,decimal TotalAmount);