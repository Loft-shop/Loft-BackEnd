using System;
using UserService.Entities;
using System.Collections.Generic;

namespace CartService.Entities;

public class Cart
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public User? Customer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}